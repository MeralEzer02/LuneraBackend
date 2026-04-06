using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TheSocialMediaV2.API.Data;
using TheSocialMediaV2.API.Tests.Fixtures;
using TheSocialMediaV2.Domain.Entities;
using Xunit;

namespace TheSocialMediaV2.Tests.Infrastructure
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
            // Arrange
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

            // Act
            var exception = await Record.ExceptionAsync(() => context.Database.ExecuteSqlRawAsync(sql, parameters));

            // Assert
            exception.Should().NotBeNull("Bir hata fırlatılmalıydı.");

            var sqlEx = GetSqlException(exception);
            sqlEx.Should().NotBeNull("Hata doğrudan veya InnerException olarak bir SqlException içermelidir.");
            sqlEx.Message.Should().Contain("CK_Match_UserNormalization",
                "Veritabanı seviyesinde UserAId'nin UserBId'den küçük olması fiziksel olarak zorunludur.");
        }

        [Fact]
        public async Task Insert_With_Invalid_TTL_Should_Throw_ConstraintViolation()
        {
            // Arrange
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

            // Act
            var exception = await Record.ExceptionAsync(() => context.Database.ExecuteSqlRawAsync(sql, parameters));

            // Assert
            exception.Should().NotBeNull("Bir hata fırlatılmalıydı.");

            var sqlEx = GetSqlException(exception);
            sqlEx.Should().NotBeNull("Hata doğrudan veya InnerException olarak bir SqlException içermelidir.");
            sqlEx.Message.Should().Contain("CK_Match_TTL",
                "Veritabanı geçmişe dönük son kullanma tarihini fiziksel olarak reddetmelidir.");
        }

        [Fact]
        public async Task Insert_With_Negative_RetryCount_Should_Throw()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var now = DateTime.UtcNow;

            var sql = @"
                INSERT INTO OutboxMessages (Id, RetryCount, OccurredOnUtc, Type, Data)
                VALUES (NEWID(), @RetryCount, @OccurredOnUtc, 'test_type', 'test_data')";

            var parameters = new[]
            {
                new SqlParameter("@RetryCount", -1),
                new SqlParameter("@OccurredOnUtc", now)
            };

            // Act
            var exception = await Record.ExceptionAsync(() => context.Database.ExecuteSqlRawAsync(sql, parameters));

            // Assert
            exception.Should().NotBeNull("Bir hata fırlatılmalıydı.");

            var sqlEx = GetSqlException(exception);
            sqlEx.Should().NotBeNull("Hata doğrudan veya InnerException olarak bir SqlException içermelidir.");
            sqlEx.Message.Should().Contain("CK_Outbox_RetryCount",
                "Outbox tablosundaki tekrar deneme sayısı sıfırın altına düşemez.");
        }

        [Fact]
        public async Task Insert_With_Valid_Data_Should_Succeed()
        {
            // Arrange
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

            // Act
            var affectedRows = await context.Database.ExecuteSqlRawAsync(sql, parameters);

            // Assert
            affectedRows.Should().BeGreaterThan(0, "Kurallara tam uyan veri, veritabanına sorunsuz yazılabilmelidir.");
        }
    }
}