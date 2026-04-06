using MediatR;

namespace TheSocialMediaV2.Application.Matches.Commands
{
    public record AcceptMatchCommand(int MatchId, int UserId) : IRequest;
}