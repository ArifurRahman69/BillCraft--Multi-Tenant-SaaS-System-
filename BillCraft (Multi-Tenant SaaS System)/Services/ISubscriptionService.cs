using BillCraft.Web.Models;

namespace BillCraft.Web.Services
{
    public interface ISubscriptionService
    {
        Task<bool> CanCreateClientAsync();
        Task<bool> CanCreateInvoiceAsync();
        Task<bool> CanCreateProductAsync();
        Task<SubscriptionUsageDto> GetSubscriptionUsageAsync();
    }
}