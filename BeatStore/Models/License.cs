using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeatStore.Models
{
    public class License
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0, 10000)]
        public decimal Price { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        // Связь с Beat
        public int BeatId { get; set; }

        [ForeignKey("BeatId")]
        public Beat Beat { get; set; }= null!;
    }
}
