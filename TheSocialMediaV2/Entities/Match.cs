using System;

namespace TheSocialMediaV2.Entities
{
    public class Match
    {
        public int Id { get; set; }
        public int UserAId { get; set; }
        public int UserBId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? EndedAt { get; set; }
        public int Status { get; set; } // 1: Active, 0: Ended
    }
}