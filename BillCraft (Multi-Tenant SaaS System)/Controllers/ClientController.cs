using BillCraft.Web.Data;
using BillCraft.Web.Models;
using BillCraft.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BillCraft.Web.Controllers
{
    [Authorize]
    public class ClientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ISubscriptionService _subscriptionService;

        public ClientController(ApplicationDbContext context, ISubscriptionService subscriptionService)
        {
            _context = context;
            _subscriptionService = subscriptionService;
        }

        // GET: Client/Index
        public async Task<IActionResult> Index()
        {
            // শুধুমাত্র অ্যাক্টিভ (IsActive = true) ক্লায়েন্টদের নিয়ে আসবে
            var clients = await _context.Clients
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(clients);
        }

        // GET: Client/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Client/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Client client)
        {
            // ১. সাবস্ক্রিপশন প্ল্যান লিমিট চেক
            bool canCreate = await _subscriptionService.CanCreateClientAsync();
            if (!canCreate)
            {
                TempData["ErrorMessage"] = "আপনার বর্তমান সাবস্ক্রিপশন প্ল্যানের ক্লায়েন্ট সীমা পূর্ণ হয়ে গেছে! নতুন ক্লায়েন্ট যোগ করতে প্ল্যান আপগ্রেড করুন।";
                return RedirectToAction("Index", "Subscription");
            }

            var tenantId = User.FindFirst("TenantId")?.Value;
            if (!string.IsNullOrEmpty(tenantId))
            {
                client.TenantId = tenantId;
            }

            // TenantId মডেলে অ্যাসাইন করার পর ModelState re-validate নিশ্চিত করা
            ModelState.Clear();
            TryValidateModel(client);

            if (!ModelState.IsValid)
            {
                return View(client);
            }

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "ক্লায়েন্ট সফলভাবে যুক্ত করা হয়েছে!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Client/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var client = await _context.Clients.FindAsync(id);
            if (client == null || !client.IsActive) return NotFound();

            return View(client);
        }

        // POST: Client/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Client client)
        {
            if (id != client.ClientId) return NotFound();

            var existingClient = await _context.Clients.FindAsync(id);
            if (existingClient == null || !existingClient.IsActive) return NotFound();

            // নাম, কোম্পানি নাম, ফোন, ইমেইল ও এড্রেস আপডেট করা
            existingClient.Name = client.Name;
            existingClient.CompanyName = client.CompanyName;
            existingClient.Phone = client.Phone;
            existingClient.Email = client.Email;
            existingClient.Address = client.Address;

            ModelState.Clear();
            TryValidateModel(existingClient);

            if (!ModelState.IsValid)
            {
                return View(client);
            }

            try
            {
                _context.Update(existingClient);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "ক্লায়েন্টের তথ্য সফলভাবে আপডেট করা হয়েছে!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Clients.AnyAsync(e => e.ClientId == client.ClientId))
                    return NotFound();
                else
                    throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Client/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client != null)
            {
                // Soft Delete: ডাটাবেজ থেকে মুছে ফেলার বদলে IsActive = false করা হচ্ছে
                client.IsActive = false;
                _context.Clients.Update(client);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "ক্লায়েন্ট সফলভাবে অপসারিত হয়েছে!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}