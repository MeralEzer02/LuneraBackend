using System.ComponentModel.DataAnnotations;
namespace Lunera.API.DTOs
{
    public class BanUserDto
    {
        [Required]
        public string Reason { get; set; } = string.Empty;
        public int? DurationInDays { get; set; }
    }
}
