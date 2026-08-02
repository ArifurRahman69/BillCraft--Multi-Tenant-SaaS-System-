namespace BillCraft.Web.Models
{
    public class SubscriptionUsageDto
    {
        public string PlanName { get; set; } = string.Empty;
        public DateTime EndDate { get; set; }

        // Client Limits
        public int ClientCount { get; set; }
        public int MaxClients { get; set; }
        public int ClientUsagePercentage => MaxClients > 0 ? (int)((double)ClientCount / MaxClients * 100) : 0;

        // Invoice Limits
        public int InvoiceCountThisMonth { get; set; }
        public int MaxInvoicesPerMonth { get; set; }
        public int InvoiceUsagePercentage => MaxInvoicesPerMonth > 0 ? (int)((double)InvoiceCountThisMonth / MaxInvoicesPerMonth * 100) : 0;

        // Product Limits
        public int ProductCount { get; set; }
        public int MaxProducts { get; set; }
        public int ProductUsagePercentage => MaxProducts > 0 ? (int)((double)ProductCount / MaxProducts * 100) : 0;
    }
}