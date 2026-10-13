namespace ProductCatalogApp.Hubs
{
    public static class MonitoringGroups
    {
        public const string AllSites = "site-all";

        public static string ForSite(int productionSiteId)
        {
            return $"site-{productionSiteId}";
        }
    }
}
