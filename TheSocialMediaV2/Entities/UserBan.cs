using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TheSocialMediaV2.API.Entities
{
    public class UserBan
    {
        protected UserBan() { }

        public UserBan(int userId, int reportId, int issuedByAdminId, string reason, int? durationDays)
        {
            UserId = userId;
            ReportId = reportId;
            IssuedByAdminId = issuedByAdminId;
            Reason = reason;
            CreatedAt = DateTime.UtcNow;

            BanUntil = durationDays.HasValue ? DateTime.UtcNow.AddDays(durationDays.Value) : null;
        }

        public int Id { get; private set; }

        [Required]
        public int UserId { get; private set; }

        [ForeignKey("UserId")]
        public virtual User User { get; private set; }

        [Required]
        public int ReportId { get; private set; } 

        [ForeignKey("ReportId")]
        public virtual Report Report { get; private set; }

        [Required]
        public int IssuedByAdminId { get; private set; }

        [ForeignKey("IssuedByAdminId")]
        public virtual User IssuedByAdmin { get; private set; }

        [Required]
        public string Reason { get; private set; } = string.Empty;

        public DateTime CreatedAt { get; private set; } 

        public DateTime? BanUntil { get; private set; } 

        public DateTime? UnbannedAt { get; private set; }
        public int? UnbannedByAdminId { get; private set; }
        public virtual User? UnbannedByAdmin { get; private set; }

        public void Revoke(int adminId)
        {
            if (UnbannedAt != null) throw new InvalidOperationException("Bu ban zaten kaldırılmış.");
            UnbannedAt = DateTime.UtcNow;
            UnbannedByAdminId = adminId;
        }

        [NotMapped]
        public bool IsActive
        {
            get
            {
                if (UnbannedAt.HasValue) return false;
                if (BanUntil == null) return true;
                return BanUntil > DateTime.UtcNow;
            }
        }
    }
}