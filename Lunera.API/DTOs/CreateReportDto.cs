using System.ComponentModel.DataAnnotations;

namespace Lunera.API.DTOs
{
    public class CreateReportDto
    {
        [Required]
        public int TargetUserId { get; set; }

        [Required]
        [MinLength(5, ErrorMessage = "Lütfen en az 5 karakterlik bir sebep belirtin.")]
        public string Reason { get; set; } = string.Empty;
    }
}