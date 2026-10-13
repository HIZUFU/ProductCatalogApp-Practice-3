namespace ProductCatalogApp.Models
{
    public class OnlineSummary
    {
        public int TotalCount { get; set; }

        public int GuestCount { get; set; }

        public List<string> UserNames { get; set; } = new();
    }
}
