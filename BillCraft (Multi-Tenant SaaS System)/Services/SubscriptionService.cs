using BillCraft.Web.Data;
using BillCraft.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace BillCraft.Web.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CanCreateClientAsync()
        {
            var sub = await GetActiveSubscriptionAsync();
            if (sub?.Plan == null) return false;

            // -1 means Unlimited
            if (sub.Plan.MaxClients == -1) return true;

            var clientCount = await _context.Clients.CountAsync(c => c.IsActive);
            return clientCount < sub.Plan.MaxClients;
        }

        public async Task<bool> CanCreateInvoiceAsync()
        {
            var sub = await GetActiveSubscriptionAsync();
            if (sub?.Plan == null) return false;

            if (sub.Plan.MaxInvoicesPerMonth == -1) return true;

            // চলতি মাসের মোট ইনভয়েস গণনা
            var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var invoiceCount = await _context.Invoices
                .Where(i => i.CreatedAt >= firstDayOfMonth)
                .CountAsync();

            return invoiceCount < sub.Plan.MaxInvoicesPerMonth;
        }

        public async Task<bool> CanCreateProductAsync()
        {
            var sub = await GetActiveSubscriptionAsync();
            if (sub?.Plan == null) return false;

            if (sub.Plan.MaxProducts == -1) return true;

            var productCount = await _context.Products.CountAsync();
            return productCount < sub.Plan.MaxProducts;
        }

        public async Task<SubscriptionUsageDto> GetSubscriptionUsageAsync()
        {
            var sub = await GetActiveSubscriptionAsync();
            var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            var clientCount = await _context.Clients.CountAsync(c => c.IsActive);
            var productCount = await _context.Products.CountAsync();
            var invoiceCount = await _context.Invoices.CountAsync(i => i.CreatedAt >= firstDayOfMonth);

            return new SubscriptionUsageDto
            {
                PlanName = sub?.Plan?.Name ?? "No Active Plan",
                EndDate = sub?.EndDate ?? DateTime.MinValue,
                ClientCount = clientCount,
                MaxClients = sub?.Plan?.MaxClients ?? 0,
                InvoiceCountThisMonth = invoiceCount,
                MaxInvoicesPerMonth = sub?.Plan?.MaxInvoicesPerMonth ?? 0,
                ProductCount = productCount,
                MaxProducts = sub?.Plan?.MaxProducts ?? 0
            };
        }

        private async Task<TenantSubscription?> GetActiveSubscriptionAsync()
        {
            return await _context.TenantSubscriptions
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.IsActive && s.EndDate >= DateTime.UtcNow);
        }
    }
}