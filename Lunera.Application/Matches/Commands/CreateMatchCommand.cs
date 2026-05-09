using MediatR;

namespace Lunera.Application.Matches.Commands
{
    public record CreateMatchCommand(int UserAId, int UserBId) : IRequest<int>;
}