using System;

namespace TheSocialMediaV2.Entities
{
    public class Report
    {
        public int Id { get; set; }
        public int ReporterUserId { get; set; }
        public int ReportedUserId { get; set; }
        public int MatchId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}