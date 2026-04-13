namespace Lunera.API.DTOs
{
    public class SendMessageDto
    {
        public int MatchId { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}