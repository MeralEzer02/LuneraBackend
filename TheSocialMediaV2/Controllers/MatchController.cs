using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TheSocialMediaV2.API.Data;
using TheSocialMediaV2.API.Entities;
using TheSocialMediaV2.API.Enums;

namespace TheSocialMediaV2.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MatchController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MatchController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/match/find
        [HttpPost("find")]
        public async Task<IActionResult> FindMatch()
        {
            var myIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(myIdStr)) return Unauthorized();
            int myId = int.Parse(myIdStr);

            var existingMatchUserIds = await _context.Matches
                .Where(m => m.UserAId == myId || m.UserBId == myId)
                .Select(m => m.UserAId == myId ? m.UserBId : m.UserAId)
                .ToListAsync();

            // 3. Aday Havuzunu Oluştur (Kendim hariç, Banlılar hariç, Eskiler hariç)
            // Not: Gerçek hayatta bu sorgu "UserProfile" üzerinden cinsiyet filtresiyle yapılır.
            // Şimdilik MVP gereği rastgele getiriyoruz.
            var candidates = await _context.Users
                .Where(u => u.Id != myId &&
                            u.Status == 1 &&
                            !existingMatchUserIds.Contains(u.Id))
                .Include(u => u.UserProfile)
                .ToListAsync();

            if (!candidates.Any())
            {
                return NotFound("Maalesef şu an kriterlerine uygun kimse yok. Biraz bekle!");
            }

            var random = new Random();
            var luckyWinner = candidates[random.Next(candidates.Count)];

            var newMatch = new Match
            {
                UserAId = myId,
                UserBId = luckyWinner.Id,
                CreatedAt = DateTime.Now,
                Status = MatchStatus.Active
            };

            _context.Matches.Add(newMatch);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Eşleşme Başarılı! 🎉",
                MatchId = newMatch.Id,
                MatchedUser = new
                {
                    Id = luckyWinner.Id,
                    Nickname = luckyWinner.UserProfile?.Nickname ?? "Anonim",
                    RealName = luckyWinner.UserProfile?.RealName,
                    Bio = luckyWinner.UserProfile?.Bio
                }
            });
        }
    }
}