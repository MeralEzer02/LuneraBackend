using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TheSocialMediaV2.API.Data;
using TheSocialMediaV2.API.DTOs;
using TheSocialMediaV2.API.Enums;
using TheSocialMediaV2.API.Services;

namespace TheSocialMediaV2.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IAdminActionLogger _logger;

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

        // 2. KULLANICI BANLAMA (Transaction Korumalı)
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

            // --- TRANSACTION BAŞLANGICI ---
            // Veri tabanında bir işlem paketi başlatıyoruz.
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // D. İşlemi Uygula
                userToBan.Status = 2; // 2: Banned
                await _context.SaveChangesAsync(); // SQL'e gider ama COMMIT edilmezse kalıcı olmaz.

                // E. LOGLAMA
                // Logger da aynı context'i kullandığı için bu transaction'a dahildir.
                await _logger.LogAsync(adminId, AdminActionType.UserBan, "Manuel Ban İşlemi", userId);

                // F. TAAHHÜT ET (COMMIT)
                // Buraya kadar hata çıkmadıysa paketi onayla ve kaydet.
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                // HATA OLURSA GERİ AL (ROLLBACK)
                // Log yazılırken hata olsa bile kullanıcı banlanmamış gibi eski haline döner.
                await transaction.RollbackAsync();
                throw; // Hatayı fırlat (500 Error dönmesi için)
            }
            // --- TRANSACTION BİTİŞİ ---

            return Ok(new { message = $"Kullanıcı (ID: {userId}) yasaklandı." });
        }

        // 3. BAN KALDIRMA (UNBAN) - (Transaction Korumalı)
        [HttpPost("unban-user/{userId}")]
        public async Task<IActionResult> UnbanUser(int userId)
        {
            // A. Kullanıcıyı bul
            var userToUnban = await _context.Users.FindAsync(userId);
            if (userToUnban == null) return NotFound("Kullanıcı bulunamadı.");

            // B. Zaten aktif mi?
            if (userToUnban.Status == 1) return BadRequest("Kullanıcı zaten aktif.");

            // --- TRANSACTION BAŞLANGICI ---
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // C. İşlemi Uygula
                userToUnban.Status = 1; // 1: Active
                await _context.SaveChangesAsync();

                // D. LOGLAMA
                var adminIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int adminId = int.Parse(adminIdStr!);

                await _logger.LogAsync(adminId, AdminActionType.UserUnban, "Manuel Ban Kaldırma", userId);

                // E. COMMIT
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                // ROLLBACK
                await transaction.RollbackAsync();
                throw;
            }
            // --- TRANSACTION BİTİŞİ ---

            return Ok(new { message = $"Kullanıcının (ID: {userId}) yasağı kaldırıldı." });
        }

        // 4. BEKLEYEN ŞİKAYETLERİ GETİR
        // GET: api/admin/reports/pending
        [HttpGet("reports/pending")]
        public async Task<IActionResult> GetPendingReports()
        {
            var reports = await _context.Reports
                .Where(r => r.Status == ReportStatus.Pending)
                .Include(r => r.Reporter)      // Şikayet eden
                .Include(r => r.ReportedUser)  // Şikayet edilen
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    ReporterName = r.Reporter.UserProfile != null ? r.Reporter.UserProfile.Nickname : "Anonim",
                    ReportedUserName = r.ReportedUser.UserProfile != null ? r.ReportedUser.UserProfile.Nickname : "Anonim",
                    ReportedUserId = r.ReportedUserId,
                    r.Reason,
                    r.CreatedAt
                })
                .ToListAsync();

            return Ok(reports);
        }

        // 5. ŞİKAYETİ SONUÇLANDIR (Transaction & Loglu)
        // POST: api/admin/resolve-report
        [HttpPost("resolve-report")]
        public async Task<IActionResult> ResolveReport([FromBody] ResolveReportDto dto)
        {
            // A. Raporu Bul
            var report = await _context.Reports.FindAsync(dto.ReportId);
            if (report == null) return NotFound("Rapor bulunamadı.");

            // B. Zaten çözülmüş mü?
            if (report.Status != ReportStatus.Pending)
                return BadRequest("Bu rapor zaten sonuçlandırılmış.");

            // C. Admin Kimliği
            var adminIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int adminId = int.Parse(adminIdStr!);

            // --- TRANSACTION BAŞLANGICI ---
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // D. Raporu Güncelle
                report.Status = dto.IsAccepted ? ReportStatus.Accepted : ReportStatus.Rejected;
                report.AdminNotes = dto.AdminNotes;
                report.ProcessedByAdminId = adminId;
                report.ProcessedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // E. LOGLAMA (Karara göre log tipi değişir)
                var actionType = dto.IsAccepted ? AdminActionType.ReportAccepted : AdminActionType.ReportRejected;

                await _logger.LogAsync(
                    adminId,
                    actionType,
                    $"Rapor Sonuçlandı: {dto.AdminNotes}",
                    report.ReportedUserId // Hedef: Şikayet edilen kişi
                );

                // F. COMMIT
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
            // --- TRANSACTION BİTİŞİ ---

            var resultMsg = dto.IsAccepted ? "Şikayet KABUL edildi." : "Şikayet REDDEDİLDİ.";
            return Ok(new { message = $"{resultMsg} İşlem kaydedildi." });
        }
    }   
}