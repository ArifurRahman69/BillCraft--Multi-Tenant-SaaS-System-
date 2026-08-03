using BillCraft.Web.Data;
using BillCraft__Multi_Tenant_SaaS_System_.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BillCraft__Multi_Tenant_SaaS_System_.Controllers
{
    [Authorize]
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GetCurrentTenantId()
        {
            return User.FindFirstValue("TenantId") ?? string.Empty;
        }

        public async Task<IActionResult> Index(string query)
        {
            var viewModel = new GlobalSearchResultViewModel { Query = query };

            if (string.IsNullOrWhiteSpace(query))
            {
                return View(viewModel);
            }

            var tenantId = GetCurrentTenantId();
            var searchTerm = query.Trim().ToLower();

            // Tenant Isolation সহ সার্চ কোয়েরি
            viewModel.Invoices = await _context.Invoices
                .AsNoTracking()
                .Include(i => i.Client)
                .Where(i => i.TenantId == tenantId &&
                           (i.InvoiceNumber.ToLower().Contains(searchTerm) ||
                            i.Client.Name.ToLower().Contains(searchTerm)))
                .ToListAsync();

            viewModel.Clients = await _context.Clients
                .AsNoTracking()
                .Where(c => c.TenantId == tenantId &&
                           (c.Name.ToLower().Contains(searchTerm) ||
                            c.Email.ToLower().Contains(searchTerm)))
                .ToListAsync();

            viewModel.Products = await _context.Products
                .AsNoTracking()
                .Where(p => p.TenantId == tenantId &&
                           p.Name.ToLower().Contains(searchTerm))
                .ToListAsync();

            return View(viewModel);
        }
    }
}