using System;
using System.Threading;
using System.Threading.Tasks;
using TheSocialMediaV2.Domain.Entities;

namespace TheSocialMediaV2.Application.Abstractions.Repositories
{
    public interface IMatchRepository
    {
        Task<Match?> GetByIdAsync(int id, CancellationToken ct = default);
        Task AddAsync(Match match, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}