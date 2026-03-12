using System.ComponentModel.DataAnnotations;

namespace TheSocialMediaV2.Domain.Entities
{
    public class ProcessedEvent
    {
        [Key]
        public Guid EventId { get; set; }

        [Required]
        public string EventType { get; set; } = string.Empty; 

        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}