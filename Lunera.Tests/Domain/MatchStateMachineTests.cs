using Xunit;
using FluentAssertions;
using Lunera.Domain.Events;
using Lunera.Domain.Entities;
using Lunera.Domain.Enums;

namespace Lunera.Tests.Domain
{
    public class MatchStateMachineTests
    {
        private readonly DateTime _now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Transition_Pending_To_Accepted_Should_Succeed_And_Raise_Event()
        {
            var match = Match.Create(1, 2, 24, _now);
            match.Accept(_now);

            match.Status.Should().Be(MatchStatus.Accepted);
            match.RespondedAt.Should().Be(_now);
            match.DomainEvents.Should().ContainSingle(e => e is MatchAcceptedEvent);
        }

        [Fact]
        public void Transition_Pending_To_Rejected_Should_Succeed()
        {
            var match = Match.Create(1, 2, 24, _now);
            match.Reject(_now);
            match.Status.Should().Be(MatchStatus.Rejected);
        }

        [Fact]
        public void Transition_Pending_To_Cancelled_Should_Succeed()
        {
            var match = Match.Create(1, 2, 24, _now);
            match.Cancel(_now);
            match.Status.Should().Be(MatchStatus.Cancelled);
        }

        [Fact]
        public void Transition_Accepted_To_Cancelled_Should_Succeed()
        {
            var match = Match.Create(1, 2, 24, _now);
            match.Accept(_now);
            match.Cancel(_now);
            match.Status.Should().Be(MatchStatus.Cancelled);
        }

        [Fact]
        public void Transition_Pending_To_Expired_Should_Succeed_When_Time_Passed()
        {
            var match = Match.Create(1, 2, 24, _now);

            var futureTime = _now.AddHours(25);

            match.Expire(futureTime);

            match.Status.Should().Be(MatchStatus.Expired);
            match.RespondedAt.Should().Be(futureTime);
            match.DomainEvents.Should().ContainSingle(e => e is MatchExpiredEvent);
        }

        [Fact]
        public void Transition_Accepted_To_Accepted_Should_Throw()
        {
            var match = Match.Create(1, 2, 24, _now);
            match.Accept(_now);
            Action act = () => match.Accept(_now);
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Transition_Rejected_To_Accepted_Should_Throw()
        {
            var match = Match.Create(1, 2, 24, _now);
            match.Reject(_now);
            Action act = () => match.Accept(_now);
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Transition_Cancelled_To_Accepted_Should_Throw()
        {
            var match = Match.Create(1, 2, 24, _now);
            match.Cancel(_now);
            Action act = () => match.Accept(_now);
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Transition_Expired_To_Accepted_Should_Throw()
        {
            var match = Match.Create(1, 2, 24, _now);
            var futureTime = _now.AddHours(25);
            match.Expire(futureTime);

            Action act = () => match.Accept(futureTime);
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Action_Accept_When_Time_Is_Up_Should_AutoExpire_And_Throw()
        {
            var match = Match.Create(1, 2, 24, _now);
            var futureTime = _now.AddHours(25);

            Action act = () => match.Accept(futureTime);

            act.Should().Throw<InvalidOperationException>().WithMessage("*süresi dolduğu için*");
            match.Status.Should().Be(MatchStatus.Expired);
        }
    }
}