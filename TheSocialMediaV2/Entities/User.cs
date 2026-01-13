using System;

namespace TheSocialMediaV2.API.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        // 0: Admin, 1: User
        public int RoleId { get; set; }

        // 0: Passive, 1: Active, 2: Banned (Enum ile yönetilecek)
        public int Status { get; set; }

        public int WarningCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}