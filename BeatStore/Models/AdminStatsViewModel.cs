namespace BeatStore.Models
{

    public class AdminStatsViewModel
    {
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }

        public string TopBeatTitle { get; set; }
        public int TopBeatSales { get; set; }

        public List<BeatStats> Beats { get; set; } = new();
        public List<SalesPoint> SalesByDay { get; set; } = new();
    }
    public class SalesPoint
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    } 

    public class BeatStats
    {
        public string Title { get; set; }
        public int SalesCount { get; set; }
        public decimal Revenue { get; set; }
    }

}