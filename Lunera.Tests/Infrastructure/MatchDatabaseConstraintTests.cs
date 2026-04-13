using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Lunera.API.Data;
using Lunera.API.Tests.Fixtures;
using Lunera.Domain.Entities;
using Xunit;

namespace Lunera.Tests.Infrastructure
{
    [Collection("SqlServerCollection")]
    public class MatchDatabaseConstraintTests
    {
        private readonly SqlServerFixture _fixture;

        public MatchDatabaseConstraintTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
        }

        private static SqlException GetSqlException(Exception exception)
        {
            return exception as SqlException ?? exception?.InnerException as SqlException;
        }

        [Fact]
        public async Task Insert_With_Invalid_User_Order_Should_Throw_ConstraintViolation()
        {
            using var context = _fixture.CreateContext();
            var now = DateTime.UtcNow;
            var expiresAt = now.AddHours(1);

            var sql = @"
                INSERT INTO Matches (UserAId, UserBId, Status, CreatedAt, ExpiresAt)
                VALUES (@UserAId, @UserBId, 0, @CreatedAt, @ExpiresAt)";

            var parameters = new[]
            {
                new SqlParameter("@UserAId", 5),
                new SqlParameter("@UserBId", 3),
                new SqlParameter("@CreatedAt", now),
                new SqlParameter("@ExpiresAt", expiresAt)
            };

            var exception = await Record.ExceptionAsync(() => context.Database.ExecuteSqlRawAsync(sql, parameters));

            exception.Should().NotBeNull("Bir hata fırlatılmalıydı.");
            var sqlEx = GetSqlException(exception);
            sqlEx.Should().NotBeNull();
            sqlEx.Message.Should().Contain("CK_Match_UserNormalization");
        }

        [Fact]
        public async Task Insert_With_Invalid_TTL_Should_Throw_ConstraintViolation()
        {
            using var context = _fixture.CreateContext();
            var now = DateTime.UtcNow;
            var expiresAt = now.AddHours(-1);

            var sql = @"
                INSERT INTO Matches (UserAId, UserBId, Status, CreatedAt, ExpiresAt)
                VALUES (@UserAId, @UserBId, 0, @CreatedAt, @ExpiresAt)";

            var parameters = new[]
            {
                new SqlParameter("@UserAId", 1),
                new SqlParameter("@UserBId", 2),
                new SqlParameter("@CreatedAt", now),
                new SqlParameter("@ExpiresAt", expiresAt)
            };

            var exception = await Record.ExceptionAsync(() => context.Database.ExecuteSqlRawAsync(sql, parameters));

            exception.Should().NotBeNull("Bir hata fırlatılmalıydı.");
            var sqlEx = GetSqlException(exception);
            sqlEx.Should().NotBeNull();
            sqlEx.Message.Should().Contain("CK_Match_TTL");
        }

        [Fact]
        public async Task Insert_With_Negative_RetryCount_Should_Throw()
        {
            using var context = _fixture.CreateContext();
            var now = DateTime.UtcNow;

            var sql = @"
                INSERT INTO OutboxMessages (Id, RetryCount, OccurredOnUtc, Type, Payload)
                VALUES (NEWID(), @RetryCount, @OccurredOnUtc, 'test_type', 'test_data')";

            var parameters = new[]
            {
                new SqlParameter("@RetryCount", -1),
                new SqlParameter("@OccurredOnUtc", now)
            };

            var exception = await Record.ExceptionAsync(() => context.Database.ExecuteSqlRawAsync(sql, parameters));

            exception.Should().NotBeNull("Bir hata fırlatılmalıydı.");
            var sqlEx = GetSqlException(exception);
            sqlEx.Should().NotBeNull();
            sqlEx.Message.Should().Contain("CK_Outbox_RetryCount");
        }

        [Fact]
        public async Task Insert_With_Valid_Data_Should_Succeed()
        {
            using var context = _fixture.CreateContext();
            var now = DateTime.UtcNow;
            var expiresAt = now.AddHours(24);

            var userA = new User();
            var userB = new User();
            context.Users.Add(userA);
            context.Users.Add(userB);
            await context.SaveChangesAsync();

            var sql = @"
                INSERT INTO Matches (UserAId, UserBId, Status, CreatedAt, ExpiresAt)
                VALUES (@UserAId, @UserBId, 0, @CreatedAt, @ExpiresAt)";

            var parameters = new[]
            {
                new SqlParameter("@UserAId", userA.Id),
                new SqlParameter("@UserBId", userB.Id),
                new SqlParameter("@CreatedAt", now),
                new SqlParameter("@ExpiresAt", expiresAt)
            };

            var affectedRows = await context.Database.ExecuteSqlRawAsync(sql, parameters);
            affectedRows.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Insert_Duplicate_Id_To_Outbox_Should_Throw_PrimaryKey_Violation()
        {
            using var context = _fixture.CreateContext();
            var now = DateTime.UtcNow;
            var eventId = Guid.NewGuid(); 

            var sql = @"
                INSERT INTO OutboxMessages (Id, RetryCount, OccurredOnUtc, Type, Payload)
                VALUES (@Id, 0, @OccurredOnUtc, 'test_type', 'test_data')";

            var parameters1 = new[] { new SqlParameter("@Id", eventId), new SqlParameter("@OccurredOnUtc", now) };
            var parameters2 = new[] { new SqlParameter("@Id", eventId), new SqlParameter("@OccurredOnUtc", now) };

            await context.Database.ExecuteSqlRawAsync(sql, parameters1);

            var exception = await Record.ExceptionAsync(() => context.Database.ExecuteSqlRawAsync(sql, parameters2));

            exception.Should().NotBeNull("Primary Key (Birincil Anahtar) ihlali olmalıydı.");
            var sqlEx = GetSqlException(exception);
            sqlEx.Should().NotBeNull();

            sqlEx.Number.Should().Be(2627, "SQL Server aynı Id'nin ikinci kez yazılmasını fiziksel olarak engellemelidir.");
            sqlEx.Message.Should().Contain("PRIMARY KEY", "Aynı event iki kez kaydedilemez.");
        }

        [Fact]
        public async Task SaveChanges_When_Outbox_Insert_Fails_Should_Rollback_Match_State()
        {
            // Arrange: Önce veritabanına geçerli bir Eşleşme (Pending statüsünde) ekliyoruz.
            using var setupContext = _fixture.CreateContext();
            var now = DateTime.UtcNow;

            var userA = new User();
            var userB = new User();
            setupContext.Users.AddRange(userA, userB);
            await setupContext.SaveChangesAsync();

            var match = Match.Create(userA.Id, userB.Id, 24, now);
            setupContext.Matches.Add(match);
            await setupContext.SaveChangesAsync(); // Şu an DB'de 1 adet Pending Match var.

            // DÜZELTME: Hazırlık aşamasında oluşan (UserCreated, MatchCreated) eventlerini çöpe atıyoruz.
            // Böylece asıl test başladığında Outbox'ımız TERTEMİZ (0) olacak!
            setupContext.OutboxMessages.RemoveRange(setupContext.OutboxMessages);
            await setupContext.SaveChangesAsync();

            // KİLİT NOKTA: Outbox tablosuna yazmayı FİZİKSEL OLARAK engelleyen bir kilit (Trigger) koyuyoruz!
            await setupContext.Database.ExecuteSqlRawAsync(@"
        CREATE TRIGGER trg_PreventOutboxInsert 
        ON OutboxMessages 
        INSTEAD OF INSERT 
        AS 
        THROW 50000, 'Outbox Insert Simulated Failure!', 1;
    ");

            try
            {
                using var handlerContext = _fixture.CreateContext();
                var dbMatch = await handlerContext.Matches.FindAsync(match.Id);

                dbMatch!.Accept(now.AddHours(1));

                Func<Task> act = async () => await handlerContext.SaveChangesAsync();

                await act.Should().ThrowAsync<Exception>();

                using var verifyContext = _fixture.CreateContext();
                var verifyMatch = await verifyContext.Matches.FindAsync(match.Id);

                verifyMatch!.Status.Should().Be(Lunera.Domain.Enums.MatchStatus.Pending,
                    "İşlem ATOMİK olduğu için Outbox patladığında, Match.Accept işlemi de GERİ ALINMALIDIR!");

                var outboxCount = await verifyContext.OutboxMessages.CountAsync();
                outboxCount.Should().Be(0, "Outbox tablosuna hiçbir şey yazılamamış olmalı.");
            }
            finally
            {
                using var cleanupContext = _fixture.CreateContext();
                await cleanupContext.Database.ExecuteSqlRawAsync("DROP TRIGGER IF EXISTS trg_PreventOutboxInsert");
            }
        }
    }
}