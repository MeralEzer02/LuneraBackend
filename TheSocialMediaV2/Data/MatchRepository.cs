using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using TheSocialMediaV2.Application.Abstractions.Repositories;
using TheSocialMediaV2.Domain.Entities;
using TheSocialMediaV2.Application.Exceptions;

namespace TheSocialMediaV2.API.Data
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