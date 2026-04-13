using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using Lunera.Application.Abstractions.Repositories;
using Lunera.Domain.Entities;
using Lunera.Application.Exceptions;

namespace Lunera.API.Data
{
    public class MatchRepository : IMatchRepository
    {
        private readonly AppDbContext _context;
        public MatchRepository(AppDbContext context) { _context = context; }

        public async Task<Match?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _context.Matches.FirstOrDefaultAsync(m => m.Id == id, ct);
        }

        public async Task AddAsync(Match match, CancellationToken ct = default)
        {
            await _context.Matches.AddAsync(match, ct);
        }

        public async Task SaveChangesAsync(CancellationToken ct = default)
        {
            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException("Eşzamanlılık çakışması yaşandı.");
            }
        }
    }
}