using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace BeatStore.Models
{
    public class Beat
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;

        public int Bpm { get; set; }
        public decimal Price { get; set; }

        public string? DemoAudioPath { get; set; }
        public string? FullAudioPath { get; set; }
        public string? CoverImagePath { get; set; }
        public bool IsSold { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public List<Order> Orders { get; set; }
        public List<License>? Licenses { get; set; }
        public ICollection<Like> Likes { get; set; }
        public int PlayCount { get; set; }
        public string? Mp3AudioPatch { get; set; }
        public string? ProducerName { get; set; }
        public string? Tags { get;set; }
    }
}