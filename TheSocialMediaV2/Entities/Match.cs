using TheSocialMediaV2.API.Enums;

namespace TheSocialMediaV2.API.Entities
{
    public class Match
    {
        public int Id { get; set; }

        public int UserAId { get; set; }
        public int UserBId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? EndedAt { get; set; }

        public MatchStatus Status { get; set; }
    }
}