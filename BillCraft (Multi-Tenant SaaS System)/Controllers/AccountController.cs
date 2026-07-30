using BillCraft.Web.Data;
using BillCraft.Web.Models;
using BillCraft.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
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

            bool exists = await _context.Tenants.AnyAsync(t => t.Subdomain == model.Subdomain.ToLower());
            if (exists)
            {
                ModelState.AddModelError("Subdomain", "এই Subdomain Key টি ইতিমধ্যেই ব্যবহৃত হয়েছে।");
                return View(model);
            }

            var defaultPlan = await _context.SubscriptionPlans.FirstOrDefaultAsync();
            if (defaultPlan == null)
            {
                defaultPlan = new SubscriptionPlan { Name = "Basic Plan", Price = 0 };
                _context.SubscriptionPlans.Add(defaultPlan);
                await _context.SaveChangesAsync();
            }

            var tenantId = Guid.NewGuid().ToString();

            var tenant = new Tenant
            {
                TenantId = tenantId,
                CompanyName = model.CompanyName,
                Subdomain = model.Subdomain.ToLower(),
                PlanId = defaultPlan.PlanId,
                Status = "Active"
            };

            var setting = new TenantSetting
            {
                TenantId = tenantId,
                Currency = "BDT",
                CompanyAddress = "Default Address"
            };

            var user = new User
            {
                TenantId = tenantId,
                FullName = model.FullName,
                Email = model.Email.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = "TenantAdmin",
                IsActive = true
            };

            _context.Tenants.Add(tenant);
            _context.TenantSettings.Add(setting);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "রেজিস্ট্রেশন সফল হয়েছে! এখন লগইন করুন।";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email.ToLower());

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "ইমেইল অথবা পাসওয়ার্ড ভুল!");
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
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("BillCraftCookie");
            return RedirectToAction(nameof(Login));
        }
    }
}