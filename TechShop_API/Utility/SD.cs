namespace TechShop_API.Utility
{
    public static class SD
    {
        public const string Role_Admin = "admin";
        public const string Role_Customer = "customer";

        public const string status_pending = "Pending";
        public const string status_confirmed = "Confirmed";
        public const string status_prepearing = "Prepearing";
        public const string status_shipment = "Shipment";
        public const string status_delivered = "Delivered";
        public const string status_cancelled = "Cancelled";

        public enum DataTypeEnum
        {
            String = 1,
            Integer = 2,
            Decimal = 3,
            Boolean = 4
        }
    }
}
