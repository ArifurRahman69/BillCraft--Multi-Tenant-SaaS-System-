using BillCraft.Web.Data;
using BillCraft.Web.Models;
using BillCraft.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BillCraft.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterTenantViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // 1. Check Duplicate Subdomain
            bool exists = await _context.Tenants.AnyAsync(t => t.Subdomain == model.Subdomain.ToLower());
            if (exists)
            {
                ModelState.AddModelError("Subdomain", "এই Subdomain Key টি ইতিমধ্যেই ব্যবহৃত হয়েছে।");
                return View(model);
            }

            // 2. Safe Fetch or Auto-Creation of Default Subscription Plan
            var defaultPlan = await _context.SubscriptionPlans.IgnoreQueryFilters().FirstOrDefaultAsync();
            if (defaultPlan == null)
            {
                defaultPlan = new SubscriptionPlan
                {
                    Name = "Basic Plan",
                    Price = 0
                };
                _context.SubscriptionPlans.Add(defaultPlan);
                await _context.SaveChangesAsync(); // Auto-generates PlanId via Identity
            }

            // 3. Create Root Tenant Entity
            var tenant = new Tenant
            {
                TenantId = Guid.NewGuid().ToString(),
                CompanyName = model.CompanyName,
                Subdomain = model.Subdomain.ToLower(),
                PlanId = defaultPlan.PlanId, // Exact Foreign Key Mapping
                Status = "Active"
            };

            // 4. Attach Navigation Properties (EF Core automatically manages Foreign Keys)
            tenant.TenantSetting = new TenantSetting
            {
                Currency = "BDT",
                CompanyAddress = "Default Address"
            };

            tenant.Users.Add(new User
            {
                FullName = model.FullName,
                Email = model.Email.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = "TenantAdmin",
                IsActive = true
            });

            // 5. Atomic Database Transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Tenants.Add(tenant); // EF Core maintains insert order automatically
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "রেজিস্ট্রেশন সফল হয়েছে! এখন লগইন করুন।";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                ModelState.AddModelError(string.Empty, "রেজিস্ট্রেশন করতে সমস্যা হয়েছে: " + errorMsg);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // IgnoreQueryFilters ব্যবহার করা হয়েছে যেন Tenant Query Filter লগইন ভ্যালিডেশন আটকায় না
            var user = await _context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == model.Email.ToLower());

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "ইমেইল অথবা পাসওয়ার্ড ভুল!");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "আপনার অ্যাকাউন্টটি নিষ্ক্রিয় করা হয়েছে। কর্তৃপক্ষের সাথে যোগাযোগ করুন।");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("TenantId", user.TenantId)
            };

            var claimsIdentity = new ClaimsIdentity(claims, "BillCraftCookie");
            var authProperties = new AuthenticationProperties { IsPersistent = model.RememberMe };

            await HttpContext.SignInAsync("BillCraftCookie", new ClaimsPrincipal(claimsIdentity), authProperties);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("BillCraftCookie");
            return RedirectToAction(nameof(Login));
        }
    }
}