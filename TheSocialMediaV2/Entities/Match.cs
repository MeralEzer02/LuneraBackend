using System.ComponentModel.DataAnnotations.Schema;
using TheSocialMediaV2.API.Enums;

namespace TheSocialMediaV2.API.Entities
{
    public class Match
    {
        public int Id { get; set; }

        public int UserAId { get; set; }
        [ForeignKey("UserAId")]
        public virtual User UserA { get; set; }

        public int UserBId { get; set; }
        [ForeignKey("UserBId")]
        public virtual User UserB { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? EndedAt { get; set; }

        public MatchStatus Status { get; set; }
    }
}