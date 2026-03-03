using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TheSocialMediaV2.API.Entities;
using TheSocialMediaV2.API.Tests.Fixtures;

namespace TheSocialMediaV2.API.Tests.Domain
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
            var match = Match.Create(1, 2, 24, _now);
            context.Matches.Add(match);
            await context.SaveChangesAsync();
            return match.Id;
        }

        [Fact]
        public async Task Test01_Real_Lost_Update_Accept_Vs_Cancel_Should_Throw()
        {
            // Arrange
            int matchId = await SeedPendingMatchAsync();

            // Act - Gerçek İzolasyon (İki farklı Scope/Context)
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
            // Arrange
            int matchId = await SeedPendingMatchAsync();
            var expireTime = _now.AddHours(25);

            // Act
            using var contextA = _fixture.CreateContext();
            using var contextB = _fixture.CreateContext();

            var matchA = await contextA.Matches.SingleAsync(m => m.Id == matchId);
            var matchB = await contextB.Matches.SingleAsync(m => m.Id == matchId);

            matchA.Accept(expireTime.AddMinutes(-1));
            await contextA.SaveChangesAsync(); 

            matchB.Expire(expireTime);

            Func<Task> act = async () => await contextB.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        }

        [Fact]
        public async Task Test03_100_Parallel_Stress_Test_Should_Only_Allow_One()
        {
            // Arrange
            int matchId = await SeedPendingMatchAsync();
            int successCount = 0;
            int concurrencyExceptionCount = 0;

            // Act - 100 Paralel Saldırı
            var tasks = Enumerable.Range(0, 100).Select(async i =>
            {
                using var context = _fixture.CreateContext();
                
                try
                {
                    var match = await context.Matches.SingleAsync(x => x.Id == matchId);
                    
                    if (match.Status == MatchStatus.Pending)
                    {
                        match.Accept(_now.AddMinutes(1));
                        await context.SaveChangesAsync();
                        Interlocked.Increment(ref successCount);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    Interlocked.Increment(ref concurrencyExceptionCount);
                }
            });

            await Task.WhenAll(tasks);

            // Assert
            successCount.Should().Be(1, "100 thread aynı anda Accept atmaya çalıştı ama sadece 1 tanesi SQL kilidini aşabilmeli.");
            concurrencyExceptionCount.Should().Be(99, "Geriye kalan 99 thread DbUpdateConcurrencyException yiyip geri dönmeli.");

            using var validationContext = _fixture.CreateContext();
            var finalMatch = await validationContext.Matches.SingleAsync(m => m.Id == matchId);
            finalMatch.Status.Should().Be(MatchStatus.Accepted);
        }
    }
}