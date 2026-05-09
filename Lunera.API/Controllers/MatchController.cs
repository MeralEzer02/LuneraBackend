using Lunera.API.Data;
using Lunera.API.DTOs;
using Lunera.Application.Matches.Commands;
using Lunera.Application.Matches.DTOs;
using Lunera.Domain.Entities;
using Lunera.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Lunera.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MatchController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMediator _mediator;

        public MatchController(AppDbContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        // POST: api/match/find
        [HttpPost("find")]
        public async Task<IActionResult> FindMatch()
        {
            var myIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(myIdStr)) return Unauthorized();
            int myId = int.Parse(myIdStr);

            var cooldownDate = DateTime.UtcNow.AddDays(-7);

            var excludedUserIds = await _context.Matches
                .Where(m => (m.UserAId == myId || m.UserBId == myId) &&
                            (
                                m.Status == MatchStatus.Pending ||
                                m.Status == MatchStatus.Accepted ||
                                m.RespondedAt > cooldownDate
                            ))
                .Select(m => m.UserAId == myId ? m.UserBId : m.UserAId)
                .ToListAsync();

            var candidates = await _context.Users
                .Where(u => u.Id != myId &&
                            u.Status == 1 &&
                            !excludedUserIds.Contains(u.Id))
                .Include(u => u.UserProfile)
                .ToListAsync();

            if (!candidates.Any())
            {
                return NotFound("Maalesef şu an kriterlerine uygun kimse yok. Biraz bekle!");
            }

            var random = new Random();
            var luckyWinner = candidates[random.Next(candidates.Count)];


            var result = new MatchResultDto
            {
                MatchId = 0,
                Message = "Aday bulundu. Karar senin! 🎯",
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

        // POST: api/match/5/accept
        [HttpPost("{id}/accept")]
        public async Task<IActionResult> Accept(int id)
        {
            var myIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(myIdStr)) return Unauthorized();
            int myId = int.Parse(myIdStr);

            var command = new AcceptMatchCommand(id, myId);

            await _mediator.Send(command, HttpContext.RequestAborted);

            return Ok(new { message = "Eşleşme başarıyla sağlandı! 🎉" });
        }

        [HttpPost("request/{targetUserId}")]
        public async Task<IActionResult> SendMatchRequest(int targetUserId)
        {
            var myIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(myIdStr)) return Unauthorized();
            int myId = int.Parse(myIdStr);

            var newMatch = Match.Create(myId, targetUserId, 24, DateTime.UtcNow);

            _context.Matches.Add(newMatch);
            await _context.SaveChangesAsync();

            return Ok(new { message = "İstek başarıyla gönderildi! Karşı tarafın onayı bekleniyor. ⏳" });
        }

        // Bekleyen eşleşmeleri (Gelen İstekleri) Listele
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var myIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(myIdStr)) return Unauthorized();
            int myId = int.Parse(myIdStr);

            var pendingMatches = await _context.Matches
                .Where(m => (m.UserAId == myId || m.UserBId == myId) && m.Status == MatchStatus.Pending)
                .Include(m => m.UserA).ThenInclude(u => u.UserProfile)
                .Include(m => m.UserB).ThenInclude(u => u.UserProfile)
                .ToListAsync();

            var result = pendingMatches.Select(m => {
                var otherUser = m.UserAId == myId ? m.UserB.UserProfile : m.UserA.UserProfile;

                return new
                {
                    MatchId = m.Id,
                    UserId = otherUser.UserId,
                    Nickname = otherUser.Nickname ?? "Anonim",
                    RealName = otherUser.RealName,
                    Bio = otherUser.Bio,
                    ExpiresAt = m.ExpiresAt
                };
            });

            return Ok(result);
        }

        // GET: api/match/active
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveMatches()
        {
            var myIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(myIdStr)) return Unauthorized();
            int myId = int.Parse(myIdStr);

            var activeMatches = await _context.Matches
                .Where(m => (m.UserAId == myId || m.UserBId == myId) && m.Status == MatchStatus.Accepted)
                .Select(m => new
                {
                    MatchId = m.Id,
                    OtherUser = m.UserAId == myId ? m.UserB.UserProfile : m.UserA.UserProfile,
                    LastMessage = _context.Messages
                        .Where(msg => msg.MatchId == m.Id)
                        .OrderByDescending(msg => msg.CreatedAt)
                        .FirstOrDefault(),
                    UnreadCount = _context.Messages
                        .Count(msg => msg.MatchId == m.Id && msg.SenderId != myId && !msg.IsRead)
                })
                .ToListAsync();

            var result = activeMatches.Select(x => new
            {
                MatchId = x.MatchId,
                UserId = x.OtherUser.UserId,
                Nickname = x.OtherUser.Nickname ?? "Anonim",
                LastMessage = x.LastMessage != null ? x.LastMessage.Content : "Henüz mesaj yok.",
                UnreadCount = x.UnreadCount
            });

            return Ok(result);
        }
    }
}