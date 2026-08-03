using BillCraft.Web.Data;
using BillCraft.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BillCraft.Web.Controllers
{
    [Authorize]
    public class SubscriptionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GetCurrentTenantId()
        {
            return User.FindFirstValue("TenantId") ?? string.Empty;
        }

        // GET: /Subscription/
        public async Task<IActionResult> Index()
        {
            var tenantId = GetCurrentTenantId();

            // ১. সব অ্যাক্টিভ প্ল্যান লোড করা
            var plans = await _context.Plans.Where(p => p.IsActive).ToListAsync();

            // ২. বর্তমান টেন্যান্টের অ্যাক্টিভ সাবস্ক্রিপশন খুঁজে বের করা (Tenant Filtering Applied)
            var currentSubscription = await _context.TenantSubscriptions
                .Where(s => s.TenantId == tenantId && s.IsActive)
                .Include(s => s.Plan)
                .FirstOrDefaultAsync();

            ViewBag.CurrentSubscription = currentSubscription;

            return View(plans);
        }

        // POST: /Subscription/Subscribe
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Subscribe(int planId)
        {
            var tenantId = GetCurrentTenantId();

            var selectedPlan = await _context.Plans.FindAsync(planId);
            if (selectedPlan == null)
            {
                TempData["ErrorMessage"] = "মনোনীত প্ল্যানটি পাওয়া যায়নি!";
                return RedirectToAction(nameof(Index));
            }

            // আগের কোনো অ্যাক্টিভ সাবস্ক্রিপশন থাকলে ইন-অ্যাক্টিভ করা (শুধুমাত্র এই টেনেটের)
            var existingSubscriptions = await _context.TenantSubscriptions
                .Where(s => s.TenantId == tenantId && s.IsActive)
                .ToListAsync();

            foreach (var sub in existingSubscriptions)
            {
                sub.IsActive = false;
            }

            // নতুন সাবস্ক্রিপশন যোগ করা
            var newSubscription = new TenantSubscription
            {
                TenantId = tenantId,
                PlanId = planId,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(selectedPlan.DurationInDays),
                IsActive = true
            };

            _context.TenantSubscriptions.Add(newSubscription);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{selectedPlan.Name} প্ল্যানটি সফলভাবে অ্যাক্টিভেট করা হয়েছে!";
            return RedirectToAction(nameof(Index));
        }
    }
}