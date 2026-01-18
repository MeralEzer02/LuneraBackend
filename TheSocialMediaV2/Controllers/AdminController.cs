using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TheSocialMediaV2.API.Data;
using TheSocialMediaV2.API.Entities;

namespace TheSocialMediaV2.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase 
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // 1. SİSTEM İSTATİSTİKLERİ Dashboard
        // GET: api/admin/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetSystemStats()
        {
            var stats = new
            {
                TotalUsers = await _context.Users.CountAsync(),
                ActiveMatches = await _context.Matches.CountAsync(m => m.Status == Enums.MatchStatus.Active),
                TotalMessages = await _context.Messages.CountAsync(),
                BannedUsers = await _context.Users.CountAsync(u => u.Status == 2)
            };

            return Ok(stats);
        }

        // 2. KULLANICI BANLAMA (Security Action)
        // POST: api/admin/ban-user/5
        [HttpPost("ban-user/{userId}")]
        public async Task<IActionResult> BanUser(int userId)
        {
            // A. Kullanıcıyı bul
            var userToBan = await _context.Users.FindAsync(userId);
            if (userToBan == null)
                return NotFound("Kullanıcı bulunamadı.");

            // B. Zaten banlı mı?
            if (userToBan.Status == 2)
                return BadRequest("Kullanıcı zaten yasaklı.");

            // C. Admin kendi kendini banlayamaz (Güvenlik Önlemi)
            // Token'dan Admin ID'sini alıyoruz
            var adminIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.Parse(adminIdStr) == userId)
                return BadRequest("Yöneticiler kendilerini yasaklayamaz.");

            // D. İşlemi Uygula
            userToBan.Status = 2; // 2: Banned

            // E. LOGLAMA (Bu işlem kritik olduğu için veritabanına yazıyoruz)
            var actionLog = new AdminActionLog
            {
                AdminId = int.Parse(adminIdStr),
                TargetUserId = userId,
                ActionType = "BAN_USER",
                CreatedAt = DateTime.Now
            };

            _context.AdminActionLogs.Add(actionLog);

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Kullanıcı (ID: {userId}) başarıyla yasaklandı ve işlem loglandı." });
        }
    }
}