using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TheSocialMediaV2.API.Data;
using TheSocialMediaV2.API.DTOs;
using TheSocialMediaV2.Domain.Entities;

namespace TheSocialMediaV2.API.Controllers
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
            // A. Kimsin?
            var myIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(myIdStr)) return Unauthorized();
            int myId = int.Parse(myIdStr);

            // B. Böyle bir eşleşme var mı?
            var match = await _context.Matches.FindAsync(dto.MatchId);
            if (match == null) return NotFound("Eşleşme bulunamadı.");

            // C. GÜVENLİK KONTROLÜ
            if (match.UserAId != myId && match.UserBId != myId)
            {
                return StatusCode(403, "Bu konuşmaya mesaj atma yetkiniz yok!");
            }

            // D. Eşleşme Aktif mi? (DÜZELTME: Accepted kontrolü)
            if (match.Status != MatchStatus.Accepted)
            {
                return BadRequest("Bu eşleşme henüz kabul edilmemiş veya sona ermiş. Mesaj atılamaz.");
            }

            // E. Mesajı Oluştur ve Kaydet
            var newMessage = new Message
            {
                MatchId = dto.MatchId,
                SenderId = myId,
                Content = dto.Content,
                CreatedAt = DateTime.Now,
                IsFlaggedByAI = false
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
                .OrderBy(m => m.CreatedAt)
                .Select(m => new MessageDto
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    IsMe = m.SenderId == myId,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                })
                .ToListAsync();

            return Ok(messages);
        }
    }
}