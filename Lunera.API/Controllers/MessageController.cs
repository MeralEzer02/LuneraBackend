using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Lunera.API.Data;
using Lunera.API.DTOs;
using Lunera.Domain.Entities;
using Lunera.Domain.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Lunera.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MessageController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MessageController(AppDbContext context)
        {
            _context = context;
        }

        // 1. MESAJ GÖNDERME
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            var myIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(myIdStr)) return Unauthorized();
            int myId = int.Parse(myIdStr);

            var match = await _context.Matches.FindAsync(dto.MatchId);
            if (match == null) return NotFound("Eşleşme bulunamadı.");

            if (match.UserAId != myId && match.UserBId != myId)
            {
                return StatusCode(403, "Bu konuşmaya mesaj atma yetkiniz yok!");
            }

            if (match.Status != MatchStatus.Accepted)
            {
                return BadRequest("Bu eşleşme henüz kabul edilmemiş veya sona ermiş. Mesaj atılamaz.");
            }

            var newMessage = new Message
            {
                MatchId = dto.MatchId,
                SenderId = myId,
                Content = dto.Content,
                CreatedAt = DateTime.Now,
                IsFlaggedByAI = false,
                IsRead = false
            };

            _context.Messages.Add(newMessage);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Mesaj gönderildi.", MessageId = newMessage.Id });
        }

        // 2. SOHBET GEÇMİŞİNİ GETİRME
        [HttpGet("{matchId}")]
        public async Task<IActionResult> GetMessages(int matchId)
        {
            var myIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int myId = int.Parse(myIdStr!);

            var match = await _context.Matches.FindAsync(matchId);
            if (match == null) return NotFound("Eşleşme bulunamadı.");

            if (match.UserAId != myId && match.UserBId != myId)
            {
                return StatusCode(403, "Bu konuşmayı görüntüleme yetkiniz yok!");
            }

            var messages = await _context.Messages
                .Where(m => m.MatchId == matchId)
                .OrderBy(m => m.Id)
                .Select(m => new
                {
                    id = m.Id,
                    senderId = m.SenderId,
                    isMe = m.SenderId == myId,
                    content = m.Content,
                    createdAt = m.CreatedAt,
                    isRead = m.IsRead
                })
                .ToListAsync();

            return Ok(messages);
        }

        // 3. OKUNDU İŞARETLEME
        [HttpPost("{matchId}/read")]
        public async Task<IActionResult> MarkMessagesAsRead(int matchId)
        {
            var myIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(myIdStr)) return Unauthorized();
            int myId = int.Parse(myIdStr);

            var unreadMessages = await _context.Messages
                .Where(m => m.MatchId == matchId && m.SenderId != myId && !m.IsRead)
                .ToListAsync();

            if (!unreadMessages.Any())
            {
                return Ok(new { message = "Okunmamış mesaj yok." });
            }

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Mesajlar okundu olarak işaretlendi." });
        }
    }
}