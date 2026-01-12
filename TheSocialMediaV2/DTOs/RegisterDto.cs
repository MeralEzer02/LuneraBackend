using System;
using System.ComponentModel.DataAnnotations;

namespace TheSocialMediaV2.DTOs
{
    public class RegisterDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string RealName { get; set; } = string.Empty;

        [Required]
        public string Nickname { get; set; } = string.Empty;

        [Required]
        public string Gender { get; set; } = string.Empty;

        [Required]
        public DateTime BirthDate { get; set; }

        public string Bio { get; set; } = string.Empty; // Zorunlu değil
    }
}