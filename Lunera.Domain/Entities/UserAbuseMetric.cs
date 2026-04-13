using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lunera.Domain.Entities
{
    public class UserAbuseMetric
    {
        [Key]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        // --- İSTATİSTİKLER ---
        public int TotalReportsReceived { get; set; } = 0; 
        public int TotalReportsConfirmed { get; set; } = 0;
        public int TotalReportsRejected { get; set; } = 0; 

        public int TotalWarnings { get; set; } = 0;        
        public int TotalBans { get; set; } = 0;            

        public DateTime? LastBanDate { get; set; }         

        // --- RİSK PUANLAMASI ---
        public int AbuseScore { get; set; } = 0;           

        public RiskLevel RiskLevel { get; set; } = RiskLevel.Low; // Enum (Low, Medium, High, Critical)

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    public enum RiskLevel
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }
}