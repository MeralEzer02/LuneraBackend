namespace TheSocialMediaV2.API.DTOs
{
    public class SendMessageDto
    {
        public int MatchId { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}