using BillCraft.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BillCraft.Web.Services
{
    public class InvoiceOverdueCheckerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<InvoiceOverdueCheckerService> _logger;

        public InvoiceOverdueCheckerService(IServiceScopeFactory scopeFactory, ILogger<InvoiceOverdueCheckerService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Overdue Invoice Checker Service শুরু হয়েছে।");

            // সার্ভিস শুরু হওয়া মাত্রই একবার বর্তমান Overdue চেক সম্পূর্ণ করে নেয়া
            try
            {
                await UpdateOverdueInvoicesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "সার্ভিস স্টার্টআপের সময় Overdue ইনভয়েস আপডেট করতে ব্যর্থ হয়েছে।");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextRunTime = now.Date.AddDays(1); // আগামীকালের রাত ১২:০০:০০ AM
                var delay = nextRunTime - now;

                _logger.LogInformation($"পরবর্তী Overdue চেক রান হবে: {nextRunTime:yyyy-MM-dd HH:mm:ss} (বাকি সময়: {delay.Hours} ঘণ্টা {delay.Minutes} মিনিট)");

                try
                {
                    await Task.Delay(delay, stoppingToken);

                    // রাত ১২টা বাজলে আপডেট লজিক রান করবে
                    await UpdateOverdueInvoicesAsync();
                }
                catch (TaskCanceledException)
                {
                    // সার্ভিস স্টপ হলে নরমাল এক্সিট
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Overdue ইনভয়েস আপডেট করার সময় সমস্যা হয়েছে।");
                }
            }
        }

        private async Task UpdateOverdueInvoicesAsync()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var today = DateTime.Today;

                // যেসব ইনভয়েসের DueDate পার হয়ে গেছে কিন্তু এখনও Unpaid বা Partially Paid রয়েছে
                var overdueInvoices = await context.Invoices
                    .Where(i => i.DueDate.HasValue
                             && i.DueDate.Value.Date < today
                             && (i.Status == "Unpaid" || i.Status == "Partially Paid"))
                    .ToListAsync();

                if (overdueInvoices.Any())
                {
                    foreach (var invoice in overdueInvoices)
                    {
                        invoice.Status = "Overdue";
                    }

                    await context.SaveChangesAsync();
                    _logger.LogInformation($"{overdueInvoices.Count} টি ইনভয়েসের স্ট্যাটাস 'Overdue' করা হয়েছে।");
                }
                else
                {
                    _logger.LogInformation("কোনো ইনভয়েস Overdue করার প্রয়োজন পড়েনি।");
                }
            }
        }
    }
}