using System.ComponentModel.DataAnnotations;

namespace BeatStore.Models
{
    public class Favorite
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; } // ID пользователя
        public int BeatId { get; set; }    // ID бита

        // Связь с битом, чтобы мы могли выводить обложку и название
        public Beat? Beat { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}