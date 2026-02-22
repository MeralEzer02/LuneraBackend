using Xunit;
using FluentAssertions;
using System.Reflection;
using TheSocialMediaV2.API.Entities;
using TheSocialMediaV2.API.Events;

namespace TheSocialMediaV2.API.Tests.Domain
{
    public class MatchStateMachineTests
    {

        [Fact]
        public void Transition_Pending_To_Accepted_Should_Succeed_And_Raise_Event()
        {
            var match = Match.Create(1, 2, 24);
            match.Accept();

            match.Status.Should().Be(MatchStatus.Accepted);
            match.RespondedAt.Should().NotBeNull();
            match.DomainEvents.Should().ContainSingle(e => e is MatchAcceptedEvent);
        }

        [Fact]
        public void Transition_Pending_To_Rejected_Should_Succeed_And_Raise_Event()
        {
            var match = Match.Create(1, 2, 24);
            match.Reject();

            match.Status.Should().Be(MatchStatus.Rejected);
            match.RespondedAt.Should().NotBeNull();
            match.DomainEvents.Should().ContainSingle(e => e is MatchRejectedEvent);
        }

        [Fact]
        public void Transition_Pending_To_Cancelled_Should_Succeed_And_Raise_Event()
        {
            var match = Match.Create(1, 2, 24);
            match.Cancel();

            match.Status.Should().Be(MatchStatus.Cancelled);
            match.DomainEvents.Should().ContainSingle(e => e is MatchCancelledEvent);
        }

        [Fact]
        public void Transition_Accepted_To_Cancelled_Should_Succeed()
        {
            var match = Match.Create(1, 2, 24);
            match.Accept();
            match.Cancel();

            match.Status.Should().Be(MatchStatus.Cancelled);
            match.DomainEvents.Should().Contain(e => e is MatchCancelledEvent);
        }

        [Fact]
        public void Transition_Pending_To_Expired_Should_Succeed_When_Time_Passed()
        {
            var match = Match.Create(1, 2, 24);

            typeof(Match).GetProperty("ExpiresAt")!.SetValue(match, DateTime.UtcNow.AddHours(-1));

            match.Expire();

            match.Status.Should().Be(MatchStatus.Expired);
            match.DomainEvents.Should().ContainSingle(e => e is MatchExpiredEvent);
        }


        [Fact]
        public void Transition_Accepted_To_Accepted_Should_Throw()
        {
            var match = Match.Create(1, 2, 24);
            match.Accept();

            Action act = () => match.Accept(); 

            act.Should().Throw<InvalidOperationException>().WithMessage("*kabul edilemez*");
        }

        [Fact]
        public void Transition_Rejected_To_Accepted_Should_Throw()
        {
            var match = Match.Create(1, 2, 24);
            match.Reject();

            Action act = () => match.Accept(); 

            act.Should().Throw<InvalidOperationException>().WithMessage("*kabul edilemez*");
        }

        [Fact]
        public void Transition_Cancelled_To_Accepted_Should_Throw()
        {
            var match = Match.Create(1, 2, 24);
            match.Cancel();

            Action act = () => match.Accept();

            act.Should().Throw<InvalidOperationException>().WithMessage("*kabul edilemez*");
        }

        [Fact]
        public void Transition_Expired_To_Accepted_Should_Throw()
        {
            var match = Match.Create(1, 2, 24);
            typeof(Match).GetProperty("ExpiresAt")!.SetValue(match, DateTime.UtcNow.AddHours(-1));
            match.Expire();

            Action act = () => match.Accept();

            act.Should().Throw<InvalidOperationException>().WithMessage("*kabul edilemez*");
        }

        [Fact]
        public void Action_Accept_When_Time_Is_Up_Should_AutoExpire_And_Throw()
        {
            var match = Match.Create(1, 2, 24);
            typeof(Match).GetProperty("ExpiresAt")!.SetValue(match, DateTime.UtcNow.AddHours(-1));

            Action act = () => match.Accept();

            act.Should().Throw<InvalidOperationException>().WithMessage("*süresi dolduğu için*");

            match.Status.Should().Be(MatchStatus.Expired);
            match.DomainEvents.Should().ContainSingle(e => e is MatchExpiredEvent);
        }
    }
}