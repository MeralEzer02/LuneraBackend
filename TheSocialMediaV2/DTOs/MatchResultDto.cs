namespace TheSocialMediaV2.API.DTOs
{
    public class MatchResultDto
    {
        public int MatchId { get; set; }
        public MatchedUserDto MatchedUser { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class MatchedUserDto
    {
        public int Id { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public string? RealName { get; set; }
        public string? Bio { get; set; }
    }
}