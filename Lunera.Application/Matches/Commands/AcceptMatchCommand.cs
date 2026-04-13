using MediatR;

namespace Lunera.Application.Matches.Commands
{
    public record AcceptMatchCommand(int MatchId, int UserId) : IRequest<Unit>;
}