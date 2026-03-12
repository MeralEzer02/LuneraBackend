using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TheSocialMediaV2.Domain.Enums;

namespace TheSocialMediaV2.Domain.Entities
{
    public class Report
    {
        public int Id { get; set; }

        // 1. ŞİKAYET EDEN (Reporter)
        [Required]
        public int ReporterId { get; set; }

        [ForeignKey("ReporterId")]
        public virtual User Reporter { get; set; }

        // 2. ŞİKAYET EDİLEN (Reported)
        [Required]
        public int ReportedUserId { get; set; }

        [ForeignKey("ReportedUserId")]
        public virtual User ReportedUser { get; set; }

        // 3. ŞİKAYET DETAYI
        [Required]
        public string Reason { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 4. DURUM
        public ReportStatus Status { get; set; } = ReportStatus.Pending; // Varsayılan: Beklemede

        // 5. KARAR DETAYLARI
        public string? AdminNotes { get; set; } // "Hakaret tespit edildi" veya "Yetersiz delil"

        public int? ProcessedByAdminId { get; set; } // Kararı veren Admin

        [ForeignKey("ProcessedByAdminId")]
        public virtual User? ProcessedByAdmin { get; set; }

        public DateTime? ProcessedAt { get; set; } // Karar zamanı
    }
}