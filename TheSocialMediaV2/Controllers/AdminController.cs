using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TheSocialMediaV2.API.Data;
using TheSocialMediaV2.API.Enums;
using TheSocialMediaV2.API.Services; // <-- Servis için bunu ekledik

namespace TheSocialMediaV2.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IAdminActionLogger _logger; // <-- LOG SERVİSİ

        // Constructor Injection: Servisi içeri alıyoruz
        public AdminController(AppDbContext context, IAdminActionLogger logger)
        {
            _context = context;
            _logger = logger;
        }

        // 1. SİSTEM İSTATİSTİKLERİ
        [HttpGet("stats")]
        public async Task<IActionResult> GetSystemStats()
        {
            var stats = new
            {
                TotalUsers = await _context.Users.CountAsync(),
                ActiveMatches = await _context.Matches.CountAsync(m => m.Status == MatchStatus.Active),
                TotalMessages = await _context.Messages.CountAsync(),
                BannedUsers = await _context.Users.CountAsync(u => u.Status == 2)
            };

            return Ok(stats);
        }

        // 2. KULLANICI BANLAMA
        [HttpPost("ban-user/{userId}")]
        public async Task<IActionResult> BanUser(int userId)
        {
            // A. Kullanıcıyı bul
            var userToBan = await _context.Users.FindAsync(userId);
            if (userToBan == null) return NotFound("Kullanıcı bulunamadı.");

            // B. Zaten banlı mı?
            if (userToBan.Status == 2) return BadRequest("Kullanıcı zaten yasaklı.");

            // C. Admin kendini banlayamaz
            var adminIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int adminId = int.Parse(adminIdStr!);

            if (adminId == userId) return BadRequest("Yöneticiler kendilerini yasaklayamaz.");

            // D. İşlemi Uygula (Önce işlemi yapıp kaydediyoruz)
            userToBan.Status = 2; // 2: Banned
            await _context.SaveChangesAsync(); // Ban durumu DB'ye işlendi.

            // E. LOGLAMA (Artık tek satır!)
            // Servis arka planda logu kaydedecek.
            await _logger.LogAsync(adminId, AdminActionType.UserBan, "Manuel Ban İşlemi", userId);

            return Ok(new { message = $"Kullanıcı (ID: {userId}) yasaklandı." });
        }

        // 3. BAN KALDIRMA (UNBAN) - YENİ ÖZELLİK
        [HttpPost("unban-user/{userId}")]
        public async Task<IActionResult> UnbanUser(int userId)
        {
            // A. Kullanıcıyı bul
            var userToUnban = await _context.Users.FindAsync(userId);
            if (userToUnban == null) return NotFound("Kullanıcı bulunamadı.");

            // B. Zaten aktif mi?
            if (userToUnban.Status == 1) return BadRequest("Kullanıcı zaten aktif.");

            // C. İşlemi Uygula
            userToUnban.Status = 1; // 1: Active
            await _context.SaveChangesAsync();

            // D. LOGLAMA
            var adminIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int adminId = int.Parse(adminIdStr!);

            await _logger.LogAsync(adminId, AdminActionType.UserUnban, "Manuel Ban Kaldırma", userId);

            return Ok(new { message = $"Kullanıcının (ID: {userId}) yasağı kaldırıldı." });
        }
    }
}