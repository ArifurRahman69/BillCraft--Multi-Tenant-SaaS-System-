using BillCraft.Web.Data;
using BillCraft__Multi_Tenant_SaaS_System_.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace BillCraft__Multi_Tenant_SaaS_System_.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Claims Data
            ViewBag.UserName = User.FindFirstValue(ClaimTypes.Name);
            ViewBag.UserEmail = User.FindFirstValue(ClaimTypes.Email);
            ViewBag.UserRole = User.FindFirstValue(ClaimTypes.Role);

            var tenantIdStr = User.FindFirstValue("TenantId");
            ViewBag.TenantId = tenantIdStr;

            var chartLabels = new List<string>();
            var chartData = new List<decimal>();

            try
            {
                var now = DateTime.Now;

                // IgnoreQueryFilters() ব্যবহার করে গ্লোবাল ফিল্টার বাইপাস করা হচ্ছে যেন ডাটা নিশ্চিতভাবে আসে
                var allInvoices = await _context.Invoices.IgnoreQueryFilters().AsNoTracking().ToListAsync();
                var allClients = await _context.Clients.IgnoreQueryFilters().AsNoTracking().ToListAsync();

                // ১. Total Invoices & Active Clients
                ViewBag.TotalInvoices = allInvoices.Count;
                ViewBag.ActiveClients = allClients.Count;

                // ২. Total Due Calculation
                ViewBag.TotalDue = allInvoices.Sum(i => i.DueAmount);

                // ৩. Monthly Sales (চলতি মাসের মোট সেল, ডাটা না থাকলে সর্বমোট সেল)
                var currentMonthSales = allInvoices
                    .Where(i => i.IssueDate.Month == now.Month && i.IssueDate.Year == now.Year)
                    .Sum(i => i.TotalAmount);

                ViewBag.MonthlySales = currentMonthSales > 0 ? currentMonthSales : allInvoices.Sum(i => i.TotalAmount);

                // ৪. Chart Analytics (গত ৬ মাস)
                for (int i = 5; i >= 0; i--)
                {
                    var targetDate = now.AddMonths(-i);
                    var monthName = targetDate.ToString("MMM");

                    var monthlyRevenue = allInvoices
                        .Where(inv => inv.IssueDate.Month == targetDate.Month && inv.IssueDate.Year == targetDate.Year)
                        .Sum(inv => inv.TotalAmount);

                    chartLabels.Add(monthName);
                    chartData.Add(monthlyRevenue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");

                ViewBag.MonthlySales = 0m;
                ViewBag.TotalInvoices = 0;
                ViewBag.ActiveClients = 0;
                ViewBag.TotalDue = 0m;

                chartLabels = new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };
                chartData = new List<decimal> { 0, 0, 0, 0, 0, 0 };
            }

            ViewBag.ChartLabels = chartLabels;
            ViewBag.ChartData = chartData;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}