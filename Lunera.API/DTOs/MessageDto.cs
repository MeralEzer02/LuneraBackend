namespace Lunera.API.DTOs
{
    public class MessageDto
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public bool IsMe { get; set; }
        public string Content { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }
}