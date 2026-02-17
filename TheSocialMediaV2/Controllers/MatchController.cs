using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TheSocialMediaV2.API.Data;
using TheSocialMediaV2.API.Entities;
using TheSocialMediaV2.API.DTOs;

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

            // 3. Aday Havuzunu Oluştur
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

            var newMatch = Match.Create(myId, luckyWinner.Id, 24);

            _context.Matches.Add(newMatch);
            await _context.SaveChangesAsync();

            var result = new MatchResultDto
            {
                MatchId = newMatch.Id,
                Message = "İstek Gönderildi! Karşı tarafın onayı bekleniyor. ⏳",
                MatchedUser = new MatchedUserDto
                {
                    Id = luckyWinner.Id,
                    Nickname = luckyWinner.UserProfile?.Nickname ?? "Anonim",
                    RealName = luckyWinner.UserProfile?.RealName,
                    Bio = luckyWinner.UserProfile?.Bio
                }
            };

            return Ok(result);
        }
    }
}