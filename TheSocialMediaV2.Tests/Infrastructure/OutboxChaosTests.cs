using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using TheSocialMediaV2.API.Data;
using TheSocialMediaV2.Domain.Entities;
using TheSocialMediaV2.Tests.E2E;
using Xunit;

namespace TheSocialMediaV2.Tests.Infrastructure
{
    public class OutboxChaosTests : IClassFixture<ApiWebApplicationFactory>
    {
        private readonly ApiWebApplicationFactory _factory;

        public OutboxChaosTests(ApiWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task CHAOS_01_Insert_Duplicate_EventId_Should_Throw_DbUpdateException()
        {
            // --- 1. ARRANGE ---
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var originalEventId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var message1 = new OutboxMessage(Guid.NewGuid(), originalEventId, now, "TestEvent", "{}");
            var message2 = new OutboxMessage(Guid.NewGuid(), originalEventId, now.AddSeconds(1), "TestEvent", "{}");

            // --- 2. ACT ---
            db.OutboxMessages.Add(message1);
            await db.SaveChangesAsync();

            db.OutboxMessages.Add(message2);

            // --- 3. ASSERT ---
            Func<Task> action = async () => await db.SaveChangesAsync();

            var exception = await action.Should().ThrowAsync<DbUpdateException>("Çünkü veritabanı aynı EventId'nin ikinci kez yazılmasına ASLA izin vermemelidir!");

            exception.Which.InnerException!.Message.Should().Contain("IX_OutboxMessages_EventId");
        }

        [Fact]
        public async Task CHAOS_03_Event_Ordering_Absolute_Guarantee()
        {
            // --- 1. ARRANGE ---
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;

            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var event1 = new OutboxMessage(id1, Guid.NewGuid(), now, "TypeA", "{}");
            var event2 = new OutboxMessage(id2, Guid.NewGuid(), now, "TypeB", "{}");
            var eventPast = new OutboxMessage(Guid.NewGuid(), Guid.NewGuid(), now.AddMinutes(-1), "TypePast", "{}");

            db.OutboxMessages.AddRange(event1, event2, eventPast);
            await db.SaveChangesAsync();

            var targetIds = new[] { event1.Id, event2.Id, eventPast.Id };

            // --- 2. ACT ---
            var messages = await db.OutboxMessages
                .Where(m => m.ProcessedOnUtc == null && targetIds.Contains(m.Id))
                .OrderBy(m => m.OccurredOnUtc)
                .ThenBy(m => m.Id)
                .ToListAsync();

            // --- 3. ASSERT ---
            messages.Should().HaveCount(3);

            messages.First().Id.Should().Be(eventPast.Id, "Çünkü en eski tarihli olay kuyruğun en başında olmalıdır.");

            var concurrentIds = messages.Skip(1).Take(2).Select(m => m.Id).ToList();
            concurrentIds.Should().BeEquivalentTo(new[] { id1, id2 }, "Aynı anda olan olaylar sıralamayı bozmamalı, ID listesini birebir içermelidir.");
        }

        [Fact]
        public async Task CHAOS_05_Poison_Message_Isolation_Should_Not_Block_Queue()
        {
            // --- 1. ARRANGE ---
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var poisonMessage = new OutboxMessage(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, "PoisonType", "Bozuk Payload");
            poisonMessage.RetryCount = 6;

            var healthyMessage = new OutboxMessage(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, "HealthyType", "Sağlam Payload");

            db.OutboxMessages.AddRange(poisonMessage, healthyMessage);
            await db.SaveChangesAsync();

            // --- 2. ACT (Worker Fetch Algoritmasını Test Et) ---
            var messagesToProcess = await db.OutboxMessages
                .Where(m => m.ProcessedOnUtc == null && m.RetryCount <= 5)
                .ToListAsync();

            // --- 3. ASSERT (Matematiksel Kanıt) ---
            messagesToProcess.Should().Contain(m => m.Id == healthyMessage.Id, "Sağlam mesaj kuyruğa alınmalıdır.");
            messagesToProcess.Should().NotContain(m => m.Id == poisonMessage.Id, "Sınırı aşan zehirli mesaj, kuyruğu tıkamaması için dışarıda bırakılmalıdır (Dead Letter Queue mantığı).");
        }
    }
}