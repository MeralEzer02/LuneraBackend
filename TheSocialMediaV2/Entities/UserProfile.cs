using System;

namespace TheSocialMediaV2.Entities
{
    public class UserProfile
    {
        public int Id { get; set; }
        public int UserId { get; set; } // User tablosu ile ilişki için
        public string RealName { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }

        public int Age
        {
            get
            {
                var age = DateTime.Today.Year - BirthDate.Year;
                if (BirthDate.Date > DateTime.Today.AddYears(-age)) age--;
                return age;
            }
        }
         
        public string Zodiac { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public bool IdentityVisible { get; set; } = false;
    }
}