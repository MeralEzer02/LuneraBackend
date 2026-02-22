using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using TheSocialMediaV2.API.Data;
using TheSocialMediaV2.API.Entities;
using Xunit;

namespace TheSocialMediaV2.API.Tests.Domain
{
    public class MatchDatabaseConstraintTests
    {
        [Fact]
        public void DbContext_Should_Have_CheckConstraint_For_UserNormalization()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new AppDbContext(options);

            var designTimeModel = context.GetService<IDesignTimeModel>().Model;
            var entityType = designTimeModel.FindEntityType(typeof(Match));

            var checkConstraints = entityType!.GetCheckConstraints();

            checkConstraints.Should().ContainSingle(c =>
                c.Name == "CK_Match_UserNormalization" &&
                c.Sql == "[UserAId] < [UserBId]",
                "Veritabanında UserAId'nin UserBId'den küçük olmasını zorunlu kılan SQL Check Constraint bulunmalıdır.");
        }

        [Fact]
        public void DbContext_Should_Have_CheckConstraint_For_TTL()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new AppDbContext(options);

            var designTimeModel = context.GetService<IDesignTimeModel>().Model;
            var entityType = designTimeModel.FindEntityType(typeof(Match));

            var checkConstraints = entityType!.GetCheckConstraints();

            checkConstraints.Should().ContainSingle(c =>
                c.Name == "CK_Match_TTL" &&
                c.Sql == "[ExpiresAt] > [CreatedAt]",
                "Veritabanında ExpiresAt'in CreatedAt'ten büyük olmasını zorunlu kılan SQL Check Constraint bulunmalıdır.");
        }

        [Fact]
        public void DbContext_Should_Have_CheckConstraint_For_Outbox_RetryCount()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new AppDbContext(options);

            var designTimeModel = context.GetService<IDesignTimeModel>().Model;
            var entityType = designTimeModel.FindEntityType(typeof(OutboxMessage));

            var checkConstraints = entityType!.GetCheckConstraints();

            checkConstraints.Should().ContainSingle(c =>
                c.Name == "CK_Outbox_RetryCount" &&
                c.Sql == "[RetryCount] >= 0",
                "Veritabanında RetryCount'un 0 veya pozitif olmasını zorunlu kılan SQL Check Constraint bulunmalıdır.");
        }
    }
}