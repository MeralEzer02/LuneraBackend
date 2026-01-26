using System.ComponentModel.DataAnnotations.Schema;

namespace TheSocialMediaV2.API.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public int RoleId { get; set; }
        public int Status { get; set; }
        public int WarningCount { get; set; }
        public DateTime CreatedAt { get; set; }

        // --- İLİŞKİLER (NAVIGATIONS) ---

        // 1. Profil İlişkisi
        public virtual UserProfile UserProfile { get; set; }

        // 2. Kullanıcının "UserA" (Başlatan) olduğu eşleşmeler
        [InverseProperty("UserA")]
        public virtual ICollection<Match> MatchesAsUserA { get; set; }

        // 3. Kullanıcının "UserB" (Diğer taraf) olduğu eşleşmeler
        [InverseProperty("UserB")]
        public virtual ICollection<Match> MatchesAsUserB { get; set; }

        // 4. Kullanıcının gönderdiği mesajlar
        [InverseProperty("Sender")]
        public virtual ICollection<Message> SentMessages { get; set; }

        // --- ADMIN LOG İLİŞKİLERİ ---

        // 1. Bu kullanıcının ADMIN sıfatıyla yaptığı işlemler (Eğer adminse)
        [InverseProperty("AdminUser")]
        public virtual ICollection<AdminActionLog> ActionsAsAdmin { get; set; }

        // 2. Bu kullanıcıya YAPILAN işlemler (Banlandı, uyarıldı vb.)
        [InverseProperty("TargetUser")]
        public virtual ICollection<AdminActionLog> ActionsAsTarget { get; set; }
    }
}