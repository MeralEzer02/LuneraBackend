using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Lunera.API.Data;
using Lunera.API.Tests.Fixtures;
using Lunera.Domain.Entities;
using Xunit;

namespace Lunera.API.Tests.Domain
{
    [Collection("SqlServerCollection")]
    public class EventLifecycleTests
    {
        private readonly SqlServerFixture _fixture;

        public EventLifecycleTests(SqlServerFixture fixture) { _fixture = fixture; }

        [Fact]
        public async Task SaveChanges_Fail_Should_Not_Clear_DomainEvents()
        {
            // Arrange
            using var context = _fixture.CreateContext();
            var now = DateTime.UtcNow;

            // KİLİT NOKTA: Domain kurallarına uygun (1000 < 2000) ama DB'de OLMAYAN kullanıcılar!
            // EnsureInvariants'ı geçer, ama SQL Server Foreign Key duvarına çarpar.
            var badMatch = Match.Create(1000, 2000, 24, now);

            // Event'in oluştuğundan emin olalım
            badMatch.DomainEvents.Should().NotBeEmpty();

            context.Matches.Add(badMatch);

            // Act
            Func<Task> act = async () => await context.SaveChangesAsync();

            // Assert
            await act.Should().ThrowAsync<DbUpdateException>("Çünkü 1000 ve 2000 ID'li kullanıcılar veritabanında YOK (Foreign Key ihlali).");

            // KRİTİK İSPAT: SQL Server patlamasına rağmen Event hala Entity'nin içinde duruyor mu?
            badMatch.DomainEvents.Should().NotBeEmpty("İşlem fail olduğu için eventler SİLİNMEMELİDİR!");
        }
    }
}