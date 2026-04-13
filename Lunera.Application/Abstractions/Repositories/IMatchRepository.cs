using System;
using System.Threading;
using System.Threading.Tasks;
using Lunera.Domain.Entities;

namespace Lunera.Application.Abstractions.Repositories
{
    public interface IMatchRepository
    {
        Task<Match?> GetByIdAsync(int id, CancellationToken ct = default);
        Task AddAsync(Match match, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}