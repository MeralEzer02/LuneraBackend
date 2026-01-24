using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TheSocialMediaV2.API.Data;
using TheSocialMediaV2.API.DTOs;
using TheSocialMediaV2.API.Entities;

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
        // POST: api/message
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
            // Bu eşleşmenin taraflarından biri ben miyim?
            if (match.UserAId != myId && match.UserBId != myId)
            {
                return StatusCode(403, "Bu konuşmaya mesaj atma yetkiniz yok!");
            }

            // D. Eşleşme Aktif mi?
            if (match.Status != Enums.MatchStatus.Active)
            {
                return BadRequest("Bu eşleşme sona ermiş veya engellenmiş. Mesaj atılamaz.");
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
        // GET: api/message/{matchId}
        [HttpGet("{matchId}")]
        public async Task<IActionResult> GetMessages(int matchId)
        {
            // A. Kimsin?
            var myIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int myId = int.Parse(myIdStr!);

            // B. Eşleşmeyi Bul
            var match = await _context.Matches.FindAsync(matchId);
            if (match == null) return NotFound("Eşleşme bulunamadı.");

            // C. GÜVENLİK KONTROLÜ
            // Başkasının mesajlarını okumaya mı çalışıyorsun?
            if (match.UserAId != myId && match.UserBId != myId)
            {
                return StatusCode(403, "Bu konuşmayı görüntüleme yetkiniz yok!");
            }

            // D. GÜNCELLEME: Mesajları DTO Olarak Getir
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