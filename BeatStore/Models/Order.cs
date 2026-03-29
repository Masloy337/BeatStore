using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BeatStore.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        public int BeatId { get; set; }
        public Beat? Beat { get; set; }

        public DateTime CreatedAt { get; set; }
        public int? LicenseId { get; set; }
        public License? License { get; set; }
        public decimal Price { get; set; }
    }
}
