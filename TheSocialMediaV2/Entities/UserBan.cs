using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TheSocialMediaV2.API.Entities
{
    public class UserBan
    {
        public int Id { get; set; }

        // 1. CEZALI (Target)
        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        // 2. DAYANAK (Evidence)
        [Required]
        public int ReportId { get; set; }

        [ForeignKey("ReportId")]
        public virtual Report Report { get; set; }

        // 3. YARGIÇ (Issuer)
        [Required]
        public int IssuedByAdminId { get; set; }

        [ForeignKey("IssuedByAdminId")]
        public virtual User IssuedByAdmin { get; set; }

        // 4. CEZA DETAYLARI
        [Required]
        public string Reason { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? BanUntil { get; set; }

        // 5. AF MEKANİZMASI (Unban)
        public DateTime? UnbannedAt { get; set; } 

        public int? UnbannedByAdminId { get; set; }

        [ForeignKey("UnbannedByAdminId")]
        public virtual User? UnbannedByAdmin { get; set; }

        // 6. HESAPLANAN DURUM
        // Bu alan veritabanında YOKTUR. Sadece kod tarafında gerçeği söyler.
        [NotMapped]
        public bool IsActive
        {
            get
            {
                // Ban kaldırılmışsa -> Pasif
                if (UnbannedAt.HasValue) return false;

                // Perma ban ise ve kaldırılmamışsa -> Aktif
                if (BanUntil == null) return true;

                // Süreli ban ise ve süre henüz dolmamışsa -> Aktif
                return BanUntil > DateTime.UtcNow;
            }
        }
    }
}