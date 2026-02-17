using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TheSocialMediaV2.API.Data;
using TheSocialMediaV2.API.DTOs;
using TheSocialMediaV2.API.Entities;
using TheSocialMediaV2.API.Enums;
using TheSocialMediaV2.API.Events;
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
        private readonly IDomainEventDispatcher _dispatcher;

        public AdminController(AppDbContext context, IAdminActionLogger logger, IDomainEventDispatcher dispatcher)
        {
            _context = context;
            _logger = logger;
            _dispatcher = dispatcher;
        }

        // 1. SİSTEM İSTATİSTİKLERİ
        [HttpGet("stats")]
        public async Task<IActionResult> GetSystemStats()
        {
            var stats = new
            {
                TotalUsers = await _context.Users.CountAsync(),
                // DÜZELTME: Enum kullanımı netleştirildi (Entities.MatchStatus.Accepted)
                ActiveMatches = await _context.Matches.CountAsync(m => m.Status == Entities.MatchStatus.Accepted),
                TotalMessages = await _context.Messages.CountAsync(),
                BannedUsers = await _context.Users.CountAsync(u => u.Status == 2)
            };

            return Ok(stats);
        }

        // ... (BanUser, UnbanUser, GetPendingReports, ResolveReport metodları AYNI KALACAK) ...
        // ... (Bu metodlarda zaten hata yoktu, sadece yukarıdaki MatchStatus hatası düzeltildi) ...

        // 2. KULLANICI BANLAMA
        [HttpPost("ban-user/{userId}")]
        public async Task<IActionResult> BanUser(int userId, [FromBody] BanUserDto dto)
        {
            var userToBan = await _context.Users.FindAsync(userId);
            if (userToBan == null) return NotFound("Kullanıcı bulunamadı.");

            bool isAlreadyBanned = await _context.UserBans.AnyAsync(b =>
                b.UserId == userId &&
                b.UnbannedAt == null &&
                (b.BanUntil == null || b.BanUntil > DateTime.UtcNow));

            if (isAlreadyBanned) return Conflict(new { message = "Kullanıcının zaten aktif bir yasağı var." });

            var adminIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int adminId = int.Parse(adminIdStr!);

            if (adminId == userId) return BadRequest("Yöneticiler kendilerini yasaklayamaz.");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync<IActionResult>(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                try
                {
                    var autoReport = new Report
                    {
                        ReporterId = adminId,
                        ReportedUserId = userId,
                        Reason = "Doğrudan Admin İşlemi: " + dto.Reason,
                        Status = ReportStatus.Accepted,
                        AdminNotes = "Doğrudan ban uygulandı.",
                        ProcessedByAdminId = adminId,
                        ProcessedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Reports.Add(autoReport);
                    await _context.SaveChangesAsync();

                    var userBan = new UserBan(
                        userId,
                        autoReport.Id,
                        adminId,
                        dto.Reason,
                        dto.DurationInDays
                    );

                    _context.UserBans.Add(userBan);

                    if (userToBan != null) userToBan.Status = 2;

                    await _context.SaveChangesAsync();
                    await _logger.LogAsync(adminId, AdminActionType.UserBan, $"Ban: {dto.Reason}", userId);

                    var domainEvent = new UserBannedEvent(userId, dto.DurationInDays, dto.Reason);
                    await _dispatcher.Dispatch(domainEvent);

                    await transaction.CommitAsync();

                    string sureBilgisi = dto.DurationInDays.HasValue ? $"{dto.DurationInDays} gün" : "SONSUZ";
                    return Ok(new { message = $"Kullanıcı yasaklandı. Süre: {sureBilgisi}. Risk skoru güncellendi." });
                }
                catch (DbUpdateException ex)
                {
                    await transaction.RollbackAsync();
                    if (ex.InnerException != null && ex.InnerException.Message.Contains("IX_UserBans_UserId"))
                    {
                        return Conflict(new { message = "Veri Çakışması: Bu kullanıcının zaten aktif bir yasağı var (DB Constraint)." });
                    }
                    throw;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        // 3. BAN KALDIRMA
        [HttpPost("unban-user/{userId}")]
        public async Task<IActionResult> UnbanUser(int userId)
        {
            var userToUnban = await _context.Users.FindAsync(userId);
            if (userToUnban == null) return NotFound("Kullanıcı bulunamadı.");

            var adminIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int adminId = int.Parse(adminIdStr!);

            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                var activeBan = await _context.UserBans
                    .Where(b => b.UserId == userId &&
                                b.UnbannedAt == null &&
                                (b.BanUntil == null || b.BanUntil > DateTime.UtcNow))
                    .OrderByDescending(b => b.CreatedAt)
                    .FirstOrDefaultAsync();

                if (activeBan == null && userToUnban.Status == 1)
                {
                    return BadRequest("Kullanıcı zaten aktif ve kaldırılacak bir yasak bulunamadı.");
                }

                string logDetails = "Manuel Statü Düzeltme";

                if (activeBan != null)
                {
                    activeBan.Revoke(adminId);
                    logDetails = $"Ban Kaldırıldı (Ref Ban ID: {activeBan.Id})";
                }

                userToUnban.Status = 1;
                await _context.SaveChangesAsync();
                await _logger.LogAsync(adminId, AdminActionType.UserUnban, logDetails, userId);
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }

            return Ok(new { message = $"Kullanıcının (ID: {userId}) yasağı kaldırıldı." });
        }

        // 4. BEKLEYEN ŞİKAYETLER
        [HttpGet("reports/pending")]
        public async Task<IActionResult> GetPendingReports()
        {
            var reports = await _context.Reports
                .Where(r => r.Status == ReportStatus.Pending)
                .Include(r => r.Reporter)
                .Include(r => r.ReportedUser)
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

        // 5. ŞİKAYET SONUÇLANDIR
        [HttpPost("resolve-report")]
        public async Task<IActionResult> ResolveReport([FromBody] ResolveReportDto dto)
        {
            var report = await _context.Reports.FindAsync(dto.ReportId);
            if (report == null) return NotFound("Rapor bulunamadı.");

            if (report.Status != ReportStatus.Pending) return BadRequest("Bu rapor zaten sonuçlandırılmış.");

            var adminIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int adminId = int.Parse(adminIdStr!);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                report.Status = dto.IsAccepted ? ReportStatus.Accepted : ReportStatus.Rejected;
                report.AdminNotes = dto.AdminNotes;
                report.ProcessedByAdminId = adminId;
                report.ProcessedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var actionType = dto.IsAccepted ? AdminActionType.ReportAccepted : AdminActionType.ReportRejected;
                await _logger.LogAsync(adminId, actionType, $"Rapor Sonuçlandı: {dto.AdminNotes}", report.ReportedUserId);

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }

            var resultMsg = dto.IsAccepted ? "Şikayet KABUL edildi." : "Şikayet REDDEDİLDİ.";
            return Ok(new { message = $"{resultMsg} İşlem kaydedildi." });
        }
    }
}