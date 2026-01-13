using System;
using TheSocialMediaV2.API.Enums;
using TheSocialMediaV2.API.Utilities;

namespace TheSocialMediaV2.API.Entities
{
    public class UserProfile
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public string RealName { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;

        public Gender Gender { get; set; } = Gender.Unknown;
        public DateTime BirthDate { get; set; }

        public string Bio { get; set; } = string.Empty;
        public bool IdentityVisible { get; set; }

        public int Age => AgeCalculator.Calculate(BirthDate);
        public ZodiacSign Zodiac => ZodiacCalculator.Calculate(BirthDate);
    }
}