namespace TheSocialMediaV2.API.Entities
{
    public class Message
    {
        public int Id { get; set; }

        public int MatchId { get; set; }
        public int SenderId { get; set; }

        public string Content { get; set; } = string.Empty;

        // MVP: AI entegrasyonu için rezerv alan (Şimdilik false kalacak)
        public bool IsFlaggedByAI { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}