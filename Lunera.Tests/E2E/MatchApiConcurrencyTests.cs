using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Lunera.Application.Matches.DTOs;
using Lunera.Domain.Entities;
using Lunera.Domain.Enums;
using Xunit;

namespace Lunera.Tests.E2E
{
    public class MatchApiConcurrencyTests : IClassFixture<ApiWebApplicationFactory>
    {
        private readonly ApiWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public MatchApiConcurrencyTests(ApiWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("TestScheme");
        }

        [Fact]
        public async Task POST_AcceptMatch_Concurrently_10_Times_Should_Process_Only_Once()
        {
            // --- 1. ARRANGE  ---
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Lunera.API.Data.AppDbContext>();

            // Test verilerini oluştur
            var userA = new User();
            var userB = new User();
            db.Users.AddRange(userA, userB);
            await db.SaveChangesAsync();

            var match = Match.Create(userA.Id, userB.Id, 24, DateTime.UtcNow);
            db.Matches.Add(match);
            await db.SaveChangesAsync();

            // Outbox'ı temizle
            db.OutboxMessages.RemoveRange(db.OutboxMessages);
            await db.SaveChangesAsync();

            var matchId = match.Id;
            var requesterId = userA.Id;

            // Header'a yetkiyi ekle
            _client.DefaultRequestHeaders.Remove("TestUserId");
            _client.DefaultRequestHeaders.Add("TestUserId", requesterId.ToString());

            var requestDto = new AcceptMatchRequest(requesterId);

            // --- 2. ACT  ---
            // 10 paralel HTTP isteği fırlatıyoruz
            int requestCount = 10;
            var tasks = Enumerable.Range(0, requestCount).Select(_ =>
                _client.PostAsJsonAsync($"/api/match/{matchId}/accept", requestDto)
            );

            // Hepsinin aynı anda bitmesini bekle
            await Task.WhenAll(tasks);

            // --- 3. ASSERT  ---
            using var verifyScope = _factory.Services.CreateScope();
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<Lunera.API.Data.AppDbContext>();

            var finalMatch = await verifyDb.Matches.FindAsync(matchId);

            // 10 istek gelse de sonuç Accepted olmalı
            finalMatch!.Status.Should().Be(MatchStatus.Accepted);

            // EN KRİTİK KANIT: 10 istekten sadece 1 tanesi Outbox Event oluşturmuş olmalı
            var outboxCount = await verifyDb.OutboxMessages.CountAsync();
            outboxCount.Should().Be(1, "Concurrency kalkanı (Optimistic Locking) sayesinde sadece 1 işlem başarılı olmalıydı.");
        }
    }
}