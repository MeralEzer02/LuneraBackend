using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Lunera.Application.Matches.DTOs;
using Lunera.Domain.Entities;
using Lunera.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lunera.Tests.E2E
{
    public class AcceptMatchE2ETests : IClassFixture<ApiWebApplicationFactory>
    {
        private readonly ApiWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AcceptMatchE2ETests(ApiWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestScheme");
        }

        [Fact]
        public async Task POST_AcceptMatch_Should_Return200_AndUpdateDB_AndCreateOutboxEvent()
        {
            // ---------------------------------------------------------
            // 1. ARRANGE: Veritabanını hazırla
            // ---------------------------------------------------------
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Lunera.API.Data.AppDbContext>();

            var now = DateTime.UtcNow;
            var userA = new User();
            var userB = new User();
            db.Users.AddRange(userA, userB);
            await db.SaveChangesAsync();

            var match = Match.Create(userA.Id, userB.Id, 24, now);
            db.Matches.Add(match);
            await db.SaveChangesAsync();

            db.OutboxMessages.RemoveRange(db.OutboxMessages);
            await db.SaveChangesAsync();

            var matchId = match.Id;
            var requesterId = userA.Id;

            _client.DefaultRequestHeaders.Add("TestUserId", requesterId.ToString());

            // ---------------------------------------------------------
            // 2. ACT: Gerçek HTTP İsteği At (Controller'a çarpıyoruz)
            // ---------------------------------------------------------
            var requestDto = new AcceptMatchRequest(requesterId);
            var response = await _client.PostAsJsonAsync($"/api/match/{matchId}/accept", requestDto);

            // ---------------------------------------------------------
            // 3. ASSERT: Beklenen Sonuçların Kanıtı
            // ---------------------------------------------------------

            // KANIT 1: HTTP 200 OK
            response.StatusCode.Should().Be(HttpStatusCode.OK, "API sorunsuz bir şekilde 200 OK dönmelidir.");

            // KANIT 2: DB -> Accepted
            using var verifyScope = _factory.Services.CreateScope();
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<Lunera.API.Data.AppDbContext>();

            var finalMatch = await verifyDb.Matches.FindAsync(matchId);
            finalMatch!.Status.Should().Be(MatchStatus.Accepted, "Veritabanındaki state Accepted olarak güncellenmiş olmalıdır.");

            // KANIT 3: Outbox -> 1 Event
            var outboxCount = await verifyDb.OutboxMessages.CountAsync();
            outboxCount.Should().Be(1, "Outbox'a tam olarak 1 adet MatchAcceptedEvent yazılmış olmalıdır.");

            var errorContent = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"API PATLADI! Status Kod: {response.StatusCode} | Detay: {errorContent}");
        }

        [Fact]
        public async Task POST_AcceptMatch_By_Stranger_Should_Return400_BadRequest()
        {
            // --- 1. ARRANGE ---
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Lunera.API.Data.AppDbContext>();

            var userA = new User();
            var userB = new User();
            var stranger = new User();
            db.Users.AddRange(userA, userB, stranger);
            await db.SaveChangesAsync();

            var match = Match.Create(userA.Id, userB.Id, 24, DateTime.UtcNow);
            db.Matches.Add(match);
            await db.SaveChangesAsync();

            // İstek atan kişi Stranger (Yabancı)
            _client.DefaultRequestHeaders.Remove("TestUserId");
            _client.DefaultRequestHeaders.Add("TestUserId", stranger.Id.ToString());

            var requestDto = new AcceptMatchRequest(stranger.Id);

            // --- 2. ACT ---
            var response = await _client.PostAsJsonAsync($"/api/match/{match.Id}/accept", requestDto);

            // --- 3. ASSERT ---
            // Handler 'InvalidOperationException' fırlatacak, 
            // Middleware bunu 400 Bad Request'e çevirecek.
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest,
                "Başkasının eşleşmesini kabul etmeye çalışan yabancı bir kullanıcı reddedilmelidir.");
        }
    }
}