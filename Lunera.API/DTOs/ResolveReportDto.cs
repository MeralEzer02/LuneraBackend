using System.ComponentModel.DataAnnotations;

namespace Lunera.API.DTOs
{
    public class ResolveReportDto
    {
        [Required]
        public int ReportId { get; set; }

        [Required]
        public bool IsAccepted { get; set; } // True: Haklı (Ceza), False: Haksız (Red)

        [Required]
        public string AdminNotes { get; set; } = string.Empty; // Karar gerekçesi
    }
}