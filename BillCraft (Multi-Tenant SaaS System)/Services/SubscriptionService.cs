using BillCraft.Web.Data;
using BillCraft.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BillCraft.Web.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SubscriptionService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // লগইন থাকা ইউজারের TenantId বের করার হেলপার মেথড
        private string GetCurrentTenantId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue("TenantId") ?? string.Empty;
        }

        public async Task<bool> CanCreateClientAsync()
        {
            var sub = await GetActiveSubscriptionAsync();
            if (sub?.Plan == null) return false;

            // -1 means Unlimited
            if (sub.Plan.MaxClients == -1) return true;

            var tenantId = GetCurrentTenantId();
            var clientCount = await _context.Clients
                .CountAsync(c => c.TenantId == tenantId && c.IsActive);

            return clientCount < sub.Plan.MaxClients;
        }

        public async Task<bool> CanCreateInvoiceAsync()
        {
            var sub = await GetActiveSubscriptionAsync();
            if (sub?.Plan == null) return false;

            if (sub.Plan.MaxInvoicesPerMonth == -1) return true;

            var tenantId = GetCurrentTenantId();
            var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            // চলতি মাসে এই টেনেটের মোট ইনভয়েস গণনা
            var invoiceCount = await _context.Invoices
                .Where(i => i.TenantId == tenantId && i.CreatedAt >= firstDayOfMonth)
                .CountAsync();

            return invoiceCount < sub.Plan.MaxInvoicesPerMonth;
        }

        public async Task<bool> CanCreateProductAsync()
        {
            var sub = await GetActiveSubscriptionAsync();
            if (sub?.Plan == null) return false;

            if (sub.Plan.MaxProducts == -1) return true;

            var tenantId = GetCurrentTenantId();
            var productCount = await _context.Products
                .CountAsync(p => p.TenantId == tenantId);

            return productCount < sub.Plan.MaxProducts;
        }

        public async Task<SubscriptionUsageDto> GetSubscriptionUsageAsync()
        {
            var tenantId = GetCurrentTenantId();
            var sub = await GetActiveSubscriptionAsync();
            var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            var clientCount = await _context.Clients
                .CountAsync(c => c.TenantId == tenantId && c.IsActive);

            var productCount = await _context.Products
                .CountAsync(p => p.TenantId == tenantId);

            var invoiceCount = await _context.Invoices
                .CountAsync(i => i.TenantId == tenantId && i.CreatedAt >= firstDayOfMonth);

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
            var tenantId = GetCurrentTenantId();

            return await _context.TenantSubscriptions
                .Include(s => s.Plan)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.IsActive && s.EndDate >= DateTime.UtcNow);
        }
    }
}