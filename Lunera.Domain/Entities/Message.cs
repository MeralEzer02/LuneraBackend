using System.ComponentModel.DataAnnotations.Schema;

namespace Lunera.Domain.Entities
{
    public class Message
    {
        public int Id { get; set; }

        public int MatchId { get; set; }
        [ForeignKey("MatchId")]
        public virtual Match Match { get; set; }

        public int SenderId { get; set; }
        [ForeignKey("SenderId")]
        public virtual User Sender { get; set; }

        public string Content { get; set; } = string.Empty;

        public bool IsFlaggedByAI { get; set; }
        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; }
    }
}