using Xunit;
using FluentAssertions;
using System.Reflection;
using Lunera.Domain.Entities;
using Lunera.Domain.Enums;

namespace Lunera.Tests.Domain
{
    public class MatchInvariantTests
    {
        private readonly DateTime _now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        private readonly DateTime _expiresAt = new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc);

        private Match CreateHackedMatch(int userA, int userB, MatchStatus status, DateTime? respondedAt, DateTime expiresAt)
        {
            var match = Match.Create(1, 2, 24, _now);
            typeof(Match).GetProperty("UserAId")!.SetValue(match, userA);
            typeof(Match).GetProperty("UserBId")!.SetValue(match, userB);
            typeof(Match).GetProperty("Status")!.SetValue(match, status);
            typeof(Match).GetProperty("RespondedAt")!.SetValue(match, respondedAt);
            typeof(Match).GetProperty("ExpiresAt")!.SetValue(match, expiresAt);
            return match;
        }

        // --- RELATIONAL & TIME TESTS ---
        [Fact]
        public void Inv01_UserA_GreaterThan_UserB_Should_Throw() =>
            FluentActions.Invoking(() => CreateHackedMatch(5, 3, MatchStatus.Pending, null, _expiresAt).EnsureInvariants(_now))
                .Should().Throw<InvalidOperationException>().WithMessage("*UserAId her zaman UserBId'den küçük olmalıdır*");

        [Fact]
        public void Inv02_UserA_Equals_UserB_Should_Throw() =>
            FluentActions.Invoking(() => CreateHackedMatch(4, 4, MatchStatus.Pending, null, _expiresAt).EnsureInvariants(_now))
                .Should().Throw<InvalidOperationException>().WithMessage("*UserAId her zaman UserBId'den küçük olmalıdır*");

        [Fact]
        public void Inv03_ExpiresAt_Past_CreatedAt_Should_Throw() =>
            FluentActions.Invoking(() => CreateHackedMatch(1, 2, MatchStatus.Pending, null, _now.AddHours(-1)).EnsureInvariants(_now))
                .Should().Throw<InvalidOperationException>().WithMessage("*ExpiresAt, CreatedAt'ten büyük olmalıdır*");

        // --- PENDING STATE TESTS ---
        [Fact]
        public void Inv04_Pending_With_RespondedAt_Should_Throw() =>
            FluentActions.Invoking(() => CreateHackedMatch(1, 2, MatchStatus.Pending, _now.AddHours(1), _expiresAt).EnsureInvariants(_now))
                .Should().Throw<InvalidOperationException>().WithMessage("*Pending statüsünde RespondedAt NULL olmalıdır*");

        // --- ACCEPTED STATE TESTS ---
        [Fact]
        public void Inv05_Accepted_Without_RespondedAt_Should_Throw() =>
            FluentActions.Invoking(() => CreateHackedMatch(1, 2, MatchStatus.Accepted, null, _expiresAt).EnsureInvariants(_now))
                .Should().Throw<InvalidOperationException>().WithMessage("*Accepted statüsünde RespondedAt dolu olmalıdır*");

        [Fact]
        public void Inv06_Accepted_With_RespondedAt_After_ExpiresAt_Should_Throw() =>
            FluentActions.Invoking(() => CreateHackedMatch(1, 2, MatchStatus.Accepted, _expiresAt.AddHours(1), _expiresAt).EnsureInvariants(_now))
                .Should().Throw<InvalidOperationException>().WithMessage("*Accepted statüsünde işlem tarihi*büyük olamaz*");

        // --- REJECTED STATE TESTS ---
        [Fact]
        public void Inv07_Rejected_Without_RespondedAt_Should_Throw() =>
            FluentActions.Invoking(() => CreateHackedMatch(1, 2, MatchStatus.Rejected, null, _expiresAt).EnsureInvariants(_now))
                .Should().Throw<InvalidOperationException>().WithMessage("*Rejected statüsünde RespondedAt dolu olmalıdır*");

        [Fact]
        public void Inv08_Rejected_With_RespondedAt_After_ExpiresAt_Should_Throw() =>
            FluentActions.Invoking(() => CreateHackedMatch(1, 2, MatchStatus.Rejected, _expiresAt.AddHours(1), _expiresAt).EnsureInvariants(_now))
                .Should().Throw<InvalidOperationException>().WithMessage("*Rejected statüsünde işlem tarihi*büyük olamaz*");

        // --- CANCELLED STATE TESTS ---
        [Fact]
        public void Inv09_Cancelled_Without_RespondedAt_Should_Throw() =>
            FluentActions.Invoking(() => CreateHackedMatch(1, 2, MatchStatus.Cancelled, null, _expiresAt).EnsureInvariants(_now))
                .Should().Throw<InvalidOperationException>().WithMessage("*Cancelled statüsünde RespondedAt dolu olmalıdır*");

        [Fact]
        public void Inv10_Cancelled_With_RespondedAt_After_ExpiresAt_Should_Throw() =>
            FluentActions.Invoking(() => CreateHackedMatch(1, 2, MatchStatus.Cancelled, _expiresAt.AddHours(1), _expiresAt).EnsureInvariants(_now))
                .Should().Throw<InvalidOperationException>().WithMessage("*Cancelled statüsünde işlem tarihi*büyük olamaz*");

        // --- EXPIRED STATE TESTS (THE KILL SHOT) ---
        [Fact]
        public void Inv11_Expired_Without_RespondedAt_Should_Throw() =>
            FluentActions.Invoking(() => CreateHackedMatch(1, 2, MatchStatus.Expired, null, _expiresAt).EnsureInvariants(_now))
                .Should().Throw<InvalidOperationException>().WithMessage("*Expired statüsünde RespondedAt dolu olmalıdır*");

        [Fact]
        public void Inv12_Expired_When_UtcNow_Is_Before_ExpiresAt_Should_Throw() =>
            FluentActions.Invoking(() => CreateHackedMatch(1, 2, MatchStatus.Expired, _now, _expiresAt).EnsureInvariants(_now))
                .Should().Throw<InvalidOperationException>().WithMessage("*anlık zamanın (utcNow), ExpiresAt'i geçmiş veya ona eşit olması gerekir*");

        [Fact]
        public void Inv13_Expired_With_RespondedAt_Before_ExpiresAt_Should_Throw() =>
            FluentActions.Invoking(() => CreateHackedMatch(1, 2, MatchStatus.Expired, _now, _expiresAt).EnsureInvariants(_expiresAt.AddHours(1)))
                .Should().Throw<InvalidOperationException>().WithMessage("*RespondedAt değeri, ExpiresAt'ten küçük olamaz*");

        // --- POSITIVE (VALID) TESTS ---
        [Fact]
        public void Inv14_Valid_Pending_Should_Pass() =>
            FluentActions.Invoking(() => CreateHackedMatch(1, 2, MatchStatus.Pending, null, _expiresAt).EnsureInvariants(_now))
                .Should().NotThrow();

        [Fact]
        public void Inv15_Valid_Accepted_Should_Pass() =>
            FluentActions.Invoking(() => CreateHackedMatch(1, 2, MatchStatus.Accepted, _now.AddHours(1), _expiresAt).EnsureInvariants(_now.AddHours(1)))
                .Should().NotThrow();

        [Fact]
        public void Inv16_Valid_Expired_Should_Pass() =>
            FluentActions.Invoking(() => CreateHackedMatch(1, 2, MatchStatus.Expired, _expiresAt, _expiresAt).EnsureInvariants(_expiresAt.AddMinutes(5)))
                .Should().NotThrow();
    }
}