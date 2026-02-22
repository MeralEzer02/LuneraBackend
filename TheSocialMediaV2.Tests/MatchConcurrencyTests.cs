using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TheSocialMediaV2.API.Entities;
using TheSocialMediaV2.API.Data;

namespace TheSocialMediaV2.API.Tests.Domain
{
    public class MatchConcurrencyTests
    {
        [Fact]
        public async Task Test01_RaceCondition_Accept_And_Cancel_Should_Throw_DbUpdateConcurrencyException()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            int matchId;
            using (var setupContext = new AppDbContext(options))
            {
                var match = Match.Create(1, 2, 24);
                typeof(Match).GetProperty("RowVersion")!.SetValue(match, BitConverter.GetBytes(1L));
                setupContext.Matches.Add(match);
                await setupContext.SaveChangesAsync();
                matchId = match.Id;
            }

            using var context1 = new AppDbContext(options);
            using var context2 = new AppDbContext(options);

            var matchForUser1 = await context1.Matches.FindAsync(matchId);
            var matchForUser2 = await context2.Matches.FindAsync(matchId);

            // 1. KULLANICI İŞLEMİ (BAŞARILI)
            matchForUser1!.Accept();
            typeof(Match).GetProperty("RowVersion")!.SetValue(matchForUser1, BitConverter.GetBytes(2L));
            await context1.SaveChangesAsync();

            // 2. KULLANICI İŞLEMİ (ÇARPIŞMA!)
            matchForUser2!.Cancel();
            typeof(Match).GetProperty("RowVersion")!.SetValue(matchForUser2, BitConverter.GetBytes(3L));

            Func<Task> act = async () => await context2.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        }

        [Fact]
        public async Task Test02_Parallel_Stress_Test_100_Concurrent_Accepts_Should_Only_Allow_One()
        {
            var dbName = Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            int matchId;
            using (var setupContext = new AppDbContext(options))
            {
                var match = Match.Create(1, 2, 24);
                typeof(Match).GetProperty("RowVersion")!.SetValue(match, BitConverter.GetBytes(1L));
                setupContext.Matches.Add(match);
                await setupContext.SaveChangesAsync();
                matchId = match.Id;
            }

            int successCount = 0;
            int concurrencyExceptionCount = 0;

            var contexts = new AppDbContext[100];
            var matches = new Match[100];
            for (int i = 0; i < 100; i++)
            {
                contexts[i] = new AppDbContext(options);
                matches[i] = (await contexts[i].Matches.FindAsync(matchId))!;
            }

            var parallelTasks = Enumerable.Range(0, 100).Select(async i =>
            {
                try
                {
                    var match = matches[i];
                    match.Accept();

                    typeof(Match).GetProperty("RowVersion")!.SetValue(match, BitConverter.GetBytes(DateTime.UtcNow.Ticks + i));

                    await contexts[i].SaveChangesAsync();
                    Interlocked.Increment(ref successCount);
                }
                catch (DbUpdateConcurrencyException)
                {
                    Interlocked.Increment(ref concurrencyExceptionCount);
                }
            });

            await Task.WhenAll(parallelTasks);

            // Assert
            successCount.Should().Be(1, "100 paralel saldırıdan SADECE 1 TANESİ veritabanına yazabilmelidir.");
            concurrencyExceptionCount.Should().Be(99, "Geriye kalan 99 isteğin tamamı OCC kalkanına çarpıp geri dönmelidir.");

            foreach (var ctx in contexts) ctx.Dispose();
        }
    }
}