using Xunit;
using FluentAssertions;
using System.Reflection;
using TheSocialMediaV2.API.Entities;
using TheSocialMediaV2.API.Events;
using Microsoft.EntityFrameworkCore;
using TheSocialMediaV2.API.Data;

namespace TheSocialMediaV2.API.Tests.Domain
{
    public class MatchAggregateBoundaryTests
    {
        [Fact]
        public void Test01_Aggregate_Should_Not_Have_Public_Constructors()
        {
            var publicConstructors = typeof(Match).GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            publicConstructors.Should().BeEmpty("Aggregate sadece Factory metot ile yaratýlmalýdýr.");
        }

        [Fact]
        public void Test02_Aggregate_Should_Not_Have_Virtual_Properties_For_Lazy_Loading_Prevention()
        {
            var virtualProperties = typeof(Match).GetProperties()
                .Where(p => p.GetMethod != null && p.GetMethod.IsVirtual && !p.GetMethod.IsFinal);

            virtualProperties.Should().BeEmpty("Hiçbir property virtual olamaz, EF Proxy bypass riski sýfýrlanmalýdýr.");
        }

        [Fact]
        public void Test03_Aggregate_Properties_Should_Not_Have_Public_Setters()
        {
            var propertiesWithPublicSetters = typeof(Match).GetProperties()
                .Where(p => p.SetMethod != null && p.SetMethod.IsPublic);

            propertiesWithPublicSetters.Should().BeEmpty("State mutation sadece davranýþsal metotlar ile yapýlmalýdýr.");
        }

        [Fact]
        public void Test04_DomainEvents_Should_Throw_InvalidCastException_When_Hacked()
        {
            var match = Match.Create(1, 2, 24, DateTime.UtcNow);

            Action act = () =>
            {
                var hackedList = (List<IInternalDomainEvent>)match.DomainEvents;
                hackedList.Clear();
            };

            act.Should().Throw<InvalidCastException>("DomainEvents listesi dýþarýdan hiçbir þekilde cast edilip deðiþtirilememelidir.");
        }

        [Fact]
        public void Test05_ClearDomainEvents_Should_Not_Be_Public()
        {
            var clearMethod = typeof(Match).GetMethod("ClearDomainEvents", BindingFlags.Public | BindingFlags.Instance);
            clearMethod.Should().BeNull("ClearDomainEvents dýþarýdan (API Layer) çaðrýlamaz, sadece EF Context (internal) çaðýrabilir.");
        }

        [Fact]
        public void Test06_Create_With_Same_User_Should_Throw_DomainException()
        {
            Action act = () => Match.Create(5, 5, 24, DateTime.UtcNow);

            act.Should().Throw<InvalidOperationException>().WithMessage("*kendisiyle eþleþemez*");
        }

        [Fact]
        public void Test07_Accept_When_Not_Pending_Should_Throw()
        {
            var match = Match.Create(1, 2, 24, DateTime.UtcNow);
            match.Cancel(DateTime.UtcNow);

            Action act = () => match.Accept(DateTime.UtcNow);

            act.Should().Throw<InvalidOperationException>().WithMessage("*kabul edilemez*");
        }

        [Fact]
        public async Task Test08_EF_ChangeTracker_Bypass_Should_Throw_On_SaveChanges()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new AppDbContext(options);
            var match = Match.Create(3, 4, 24, DateTime.UtcNow);

            typeof(Match).GetProperty("RowVersion")!.SetValue(match, new byte[8]);

            context.Matches.Add(match);
            await context.SaveChangesAsync();

            context.Entry(match).Property(x => x.Status).CurrentValue = MatchStatus.Accepted;

            Func<Task> act = async () => await context.SaveChangesAsync();
            await act.Should().ThrowAsync<InvalidOperationException>()
               .WithMessage("*INVARIANT VIOLATION: Ýþlem görmüþ eþleþmede RespondedAt boþ olamaz*");
        }

        [Fact]
        public void Test09_Reflection_Hack_To_Set_State_Should_Fail_Validation()
        {
            var match = Match.Create(1, 2, 24, DateTime.UtcNow);

            var propertyInfo = typeof(Match).GetProperty("UserAId", BindingFlags.Public | BindingFlags.Instance);
            propertyInfo!.DeclaringType!.GetProperty("UserAId")!.SetValue(match, 10, BindingFlags.NonPublic | BindingFlags.SetProperty, null, null, null);

            Action act = () => match.EnsureInvariants();
            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*INVARIANT VIOLATION: UserAId her zaman UserBId'den küçük olmalýdýr*");
        }

        [Fact]
        public void Test10_Cancel_When_Already_Cancelled_Should_Throw()
        {
            var match = Match.Create(1, 2, 24, DateTime.UtcNow);
            match.Cancel(DateTime.UtcNow);

            Action act = () => match.Cancel(DateTime.UtcNow);

            act.Should().Throw<InvalidOperationException>().WithMessage("*iptal edilemez*");
        }
    }
}