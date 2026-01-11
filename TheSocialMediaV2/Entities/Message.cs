using System;

namespace TheSocialMediaV2.Entities
{
    public class Message
    {
        public int Id { get; set; }
        public int MatchId { get; set; }
        public int SenderId { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsFlaggedByAI { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}