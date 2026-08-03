using BillCraft.Web.Data;
using BillCraft.Web.Services;
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
        private readonly ISubscriptionService _subscriptionService;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context,
            ISubscriptionService subscriptionService)
        {
            _logger = logger;
            _context = context;
            _subscriptionService = subscriptionService;
        }

        private string GetCurrentTenantId()
        {
            return User.FindFirstValue("TenantId") ?? string.Empty;
        }

        public async Task<IActionResult> Index()
        {
            var tenantId = GetCurrentTenantId();

            // Claims Data
            ViewBag.UserName = User.FindFirstValue(ClaimTypes.Name);
            ViewBag.UserEmail = User.FindFirstValue(ClaimTypes.Email);
            ViewBag.UserRole = User.FindFirstValue(ClaimTypes.Role);
            ViewBag.TenantId = tenantId;

            var chartLabels = new List<string>();
            var chartRevenueData = new List<decimal>();
            var chartExpenseData = new List<decimal>();

            try
            {
                // Subscription Usage Limit Data Fetching
                var usageData = await _subscriptionService.GetSubscriptionUsageAsync();
                ViewBag.SubscriptionUsage = usageData;

                var now = DateTime.UtcNow;

                // শুধুমাত্র চলতি টেনেটের ডাটা লোড করা হচ্ছে (Tenant Isolation)
                var tenantInvoices = await _context.Invoices
                    .Where(i => i.TenantId == tenantId)
                    .AsNoTracking()
                    .ToListAsync();

                var tenantClientsCount = await _context.Clients
                    .Where(c => c.TenantId == tenantId)
                    .AsNoTracking()
                    .CountAsync();

                var tenantExpenses = await _context.Expenses
                    .Where(e => e.TenantId == tenantId)
                    .AsNoTracking()
                    .ToListAsync();

                // ১. Total Invoices & Active Clients
                ViewBag.TotalInvoices = tenantInvoices.Count;
                ViewBag.ActiveClients = tenantClientsCount;

                // ২. Total Due Calculation (বকেয়া পরিমাণ)
                ViewBag.TotalDue = tenantInvoices.Sum(i => i.TotalAmount - i.PaidAmount);

                // ৩. Monthly Sales & Expenses (চলতি মাসের হিসাব)
                var currentMonthSales = tenantInvoices
                    .Where(i => i.IssueDate.Month == now.Month && i.IssueDate.Year == now.Year)
                    .Sum(i => i.TotalAmount);

                var currentMonthExpenses = tenantExpenses
                    .Where(e => e.ExpenseDate.Month == now.Month && e.ExpenseDate.Year == now.Year)
                    .Sum(e => e.Amount);

                ViewBag.MonthlySales = currentMonthSales;
                ViewBag.MonthlyExpenses = currentMonthExpenses;

                // ৪. Net Profit Calculation
                ViewBag.NetProfit = currentMonthSales - currentMonthExpenses;

                // ৫. Chart Analytics (গত ৬ মাস: Revenue vs Expense - Fixed Year + Month matching)
                for (int i = 5; i >= 0; i--)
                {
                    var targetDate = now.AddMonths(-i);
                    var monthName = targetDate.ToString("MMM");

                    var monthlyRevenue = tenantInvoices
                        .Where(inv => inv.IssueDate.Month == targetDate.Month && inv.IssueDate.Year == targetDate.Year)
                        .Sum(inv => inv.TotalAmount);

                    var monthlyExpense = tenantExpenses
                        .Where(exp => exp.ExpenseDate.Month == targetDate.Month && exp.ExpenseDate.Year == targetDate.Year)
                        .Sum(exp => exp.Amount);

                    chartLabels.Add(monthName);
                    chartRevenueData.Add(monthlyRevenue);
                    chartExpenseData.Add(monthlyExpense);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data for tenant: {TenantId}", tenantId);

                ViewBag.MonthlySales = 0m;
                ViewBag.MonthlyExpenses = 0m;
                ViewBag.NetProfit = 0m;
                ViewBag.TotalInvoices = 0;
                ViewBag.ActiveClients = 0;
                ViewBag.TotalDue = 0m;

                chartLabels = new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };
                chartRevenueData = new List<decimal> { 0, 0, 0, 0, 0, 0 };
                chartExpenseData = new List<decimal> { 0, 0, 0, 0, 0, 0 };
            }

            ViewBag.ChartLabels = chartLabels;
            ViewBag.ChartRevenueData = chartRevenueData;
            ViewBag.ChartExpenseData = chartExpenseData;

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