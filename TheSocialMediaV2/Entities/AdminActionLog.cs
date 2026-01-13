using System;

namespace TheSocialMediaV2.API.Entities
{
    public class AdminActionLog
    {
        public int Id { get; set; }
        public int AdminId { get; set; }
        public string ActionType { get; set; } = string.Empty; // "BAN", "WARNING" vb.
        public int TargetUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}