using System.ComponentModel.DataAnnotations;

namespace BeatStore.Models
{
    public class Like
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        public int BeatId { get; set; }

        public Beat Beat { get; set; }
    }
}
