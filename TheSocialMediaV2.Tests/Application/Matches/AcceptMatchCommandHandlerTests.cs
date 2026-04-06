using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TheSocialMediaV2.Application.Abstractions.Repositories;
using TheSocialMediaV2.Application.Abstractions.Services;
using TheSocialMediaV2.Application.Matches.Commands;
using TheSocialMediaV2.Application.Matches.Handlers;
using TheSocialMediaV2.Domain.Entities;
using TheSocialMediaV2.Domain.Enums;
using Xunit;
using Match = TheSocialMediaV2.Domain.Entities.Match;

namespace TheSocialMediaV2.Tests.Application.Matches
{
    public class AcceptMatchCommandHandlerTests
    {
        private readonly Mock<IMatchRepository> _mockRepo;
        private readonly Mock<IClock> _mockClock;
        private readonly AcceptMatchCommandHandler _handler;

        public AcceptMatchCommandHandlerTests()
        {
            _mockRepo = new Mock<IMatchRepository>();
            _mockClock = new Mock<IClock>();

            _handler = new AcceptMatchCommandHandler(_mockRepo.Object, _mockClock.Object);
        }

        [Fact]
        public async Task Handle_Should_Call_Accept_And_SaveChanges()
        {
            var matchId = 1;
            var userAId = 10;
            var userBId = 20;
            var creationTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            var match = Match.Create(userAId, userBId, 24, creationTime);

            _mockRepo.Setup(x => x.GetByIdAsync(matchId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(match);

            var acceptTime = creationTime.AddHours(1);
            _mockClock.Setup(x => x.UtcNow).Returns(acceptTime);

            var command = new AcceptMatchCommand(matchId, userBId);

            await _handler.Handle(command, CancellationToken.None);

            match.Status.Should().Be(MatchStatus.Accepted);

            _mockRepo.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}