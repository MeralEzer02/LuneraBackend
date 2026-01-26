using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TheSocialMediaV2.API.Enums;

namespace TheSocialMediaV2.API.Entities
{
    public class AdminActionLog
    {
        public int Id { get; set; }

        // 1. İŞLEMİ YAPAN (ADMİN)
        [Required]
        public int AdminUserId { get; set; }

        [ForeignKey("AdminUserId")]
        public virtual User AdminUser { get; set; }

        // 2. YAPILAN İŞLEM (TÜRÜ SABİT)
        [Required]
        public AdminActionType ActionType { get; set; }

        // 3. HEDEF (KİME/NEYE YAPILDI?)
        public int? TargetUserId { get; set; } // Eğer hedef bir kullanıcıysa

        [ForeignKey("TargetUserId")]
        public virtual User TargetUser { get; set; }

        public int? TargetEntityId { get; set; } // MesajID, RaporID vb.
        public string? TargetEntityType { get; set; } // "Message", "Report"

        // 4. GEREKÇE
        [Required]
        public string Reason { get; set; } = string.Empty;

        // 5. EKSTRA BİLGİ (SNAPSHOT)
        public string? Metadata { get; set; } // İşlem anındaki verinin kopyası (JSON)

        // 6. ZAMAN DAMGASI
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}