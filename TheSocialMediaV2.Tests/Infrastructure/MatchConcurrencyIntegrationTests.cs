using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TheSocialMediaV2.API.Data;
using TheSocialMediaV2.API.Tests.Fixtures;
using TheSocialMediaV2.Domain.Entities;
using TheSocialMediaV2.Domain.Enums;
using Xunit;

namespace TheSocialMediaV2.Tests.Infrastructure
{
    [CollectionDefinition("SqlServerCollection")]
    public class SqlServerCollection : ICollectionFixture<SqlServerFixture> { }

    [Collection("SqlServerCollection")]
    public class MatchConcurrencyIntegrationTests
    {
        private readonly SqlServerFixture _fixture;
        private readonly DateTime _now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        public MatchConcurrencyIntegrationTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
        }

        private async Task<int> SeedPendingMatchAsync()
        {
            using var context = _fixture.CreateContext();

            var userA = new User();
            var userB = new User();

            context.Users.Add(userA);
            context.Users.Add(userB);
            await context.SaveChangesAsync();

            var match = Match.Create(userA.Id, userB.Id, 24, _now);
            context.Matches.Add(match);
            await context.SaveChangesAsync();

            return match.Id;
        }

        [Fact]
        public async Task Test01_Real_Lost_Update_Accept_Vs_Cancel_Should_Throw()
        {
            // Arrange
            int matchId = await SeedPendingMatchAsync();

            // Act - Gerçek İzolasyon
            using var contextA = _fixture.CreateContext();
            using var contextB = _fixture.CreateContext();

            var matchA = await contextA.Matches.SingleAsync(m => m.Id == matchId);
            var matchB = await contextB.Matches.SingleAsync(m => m.Id == matchId);

            matchA.Accept(_now.AddMinutes(1));
            await contextA.SaveChangesAsync();

            matchB.Cancel(_now.AddMinutes(2));

            Func<Task> act = async () => await contextB.SaveChangesAsync();

            await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        }

        [Fact]
        public async Task Test02_Cross_State_Race_Accept_Vs_Expire_Should_Throw()
        {
            int matchId = await SeedPendingMatchAsync();
            var limitTime = _now.AddHours(24);

            using var contextA = _fixture.CreateContext();
            using var contextB = _fixture.CreateContext();

            var matchA = await contextA.Matches.SingleAsync(m => m.Id == matchId);
            var matchB = await contextB.Matches.SingleAsync(m => m.Id == matchId);

            // Context A (Kullanıcı) sürenin dolmasına kıl payı 1 dakika kala (23h 59m) Accept atıyor
            matchA.Accept(limitTime.AddMinutes(-1));
            await contextA.SaveChangesAsync();

            // Context B (Sistem) süresi 1 dakika geçti (24h 1m) diyerek Expire atmaya çalışıyor
            matchB.Expire(limitTime.AddMinutes(1));

            // Assert - İkinci işlem (B) RowVersion uyuşmazlığından patlamalı!
            Func<Task> act = async () => await contextB.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        }

        [Fact]
        public async Task Test03_100_Parallel_Stress_Test_Should_Only_Allow_One()
        {
            int matchId = await SeedPendingMatchAsync();
            int successCount = 0;
            int concurrencyExceptionCount = 0;

            var contexts = new AppDbContext[100];
            var matches = new Match[100];

            for (int i = 0; i < 100; i++)
            {
                contexts[i] = _fixture.CreateContext();
                matches[i] = await contexts[i].Matches.SingleAsync(x => x.Id == matchId);
            }

            var tasks = Enumerable.Range(0, 100).Select(async i =>
            {
                try
                {
                    matches[i].Accept(_now.AddMinutes(1));
                    await contexts[i].SaveChangesAsync();
                    Interlocked.Increment(ref successCount);
                }
                catch (DbUpdateConcurrencyException)
                {
                    Interlocked.Increment(ref concurrencyExceptionCount);
                }
                catch (DbUpdateException)
                {
                    Interlocked.Increment(ref concurrencyExceptionCount);
                }
                finally
                {
                    contexts[i].Dispose();
                }
            });

            await Task.WhenAll(tasks);

            // Assert
            successCount.Should().Be(1, "100 thread aynı anda Accept atmaya çalıştı ama sadece 1 tanesi SQL kilidini aşabilmeli.");
            concurrencyExceptionCount.Should().Be(99, "Geriye kalan 99 thread SQL kalkanına çarpıp geri dönmeli.");

            using var validationContext = _fixture.CreateContext();
            var finalMatch = await validationContext.Matches.SingleAsync(m => m.Id == matchId);
            finalMatch.Status.Should().Be(MatchStatus.Accepted);
        }
    }
}