using MediatR;

namespace Lunera.Application.Matches.Commands
{
    // IRequest<int> -> Bu işlem bittiğinde geriye Match'in Id'sini (int) dönecek demek.
    // Senin Domain modelinde Id'ler int olduğu için int kullanıyoruz.
    public record CreateMatchCommand(int UserAId, int UserBId) : IRequest<int>;
}