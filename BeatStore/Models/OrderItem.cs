using System.ComponentModel.DataAnnotations.Schema;

namespace BeatStore.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public int BeatId { get; set; }

        public int LicenseId { get; set; }

        public decimal Price { get; set; }

        // Связи
        [ForeignKey("OrderId")]
        public Order Order { get; set; }

        [ForeignKey("BeatId")]
        public Beat Beat { get; set; }

        [ForeignKey("LicenseId")]
        public License License { get; set; }
    }
}
