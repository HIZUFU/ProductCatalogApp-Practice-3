namespace ProductCatalogApp.Hubs
{
    public static class MonitoringClientMethods
    {
        public const string OnlineChanged = "OnlineChanged";

        public const string ReceiveSnapshot = "ReceiveSnapshot";

        public const string ReceiveReading = "ReceiveReading";

        public const string AlarmRaised = "AlarmRaised";

        public const string ReadingAccepted = "ReadingAccepted";
    }
}
