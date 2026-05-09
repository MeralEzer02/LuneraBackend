using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Lunera.API.Data;
using Lunera.API.DTOs;
using Lunera.Domain.Entities;
using Lunera.Domain.Enums;

namespace Lunera.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/report
        [HttpPost]
        public async Task<IActionResult> CreateReport([FromBody] CreateReportDto dto)
        {
            // 1. Kim şikayet ediyor?
            var reporterIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int reporterId = int.Parse(reporterIdStr!);

            // 2. Kendini mi şikayet ediyor?
            if (reporterId == dto.TargetUserId)
                return BadRequest("Kendinizi şikayet edemezsiniz.");

            // 3. Hedef var mı?
            var targetUser = await _context.Users.FindAsync(dto.TargetUserId);
            if (targetUser == null)
                return NotFound("Şikayet edilen kullanıcı bulunamadı.");

            // 4. Raporu Oluştur
            var report = new Report
            {
                ReporterId = reporterId,
                ReportedUserId = dto.TargetUserId,
                Reason = dto.Reason,
                CreatedAt = DateTime.UtcNow,
                Status = ReportStatus.Pending // ÖNEMLİ: Varsayılan durum Beklemede
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Şikayetiniz alındı. İncelendikten sonra işlem yapılacaktır." });
        }
    }
}