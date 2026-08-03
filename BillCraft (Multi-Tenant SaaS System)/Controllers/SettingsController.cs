using BillCraft.Web.Data;
using BillCraft__Multi_Tenant_SaaS_System_.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BillCraft__Multi_Tenant_SaaS_System_.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public SettingsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var tenantId = User.FindFirstValue("TenantId");

            var settings = await _context.Set<TenantSettings>()
                .FirstOrDefaultAsync(s => s.TenantId == tenantId);

            if (settings == null)
            {
                settings = new TenantSettings
                {
                    TenantId = tenantId ?? string.Empty,
                    CompanyName = User.FindFirstValue(ClaimTypes.Name) ?? "My Company"
                };
            }

            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(TenantSettings model, IFormFile? logoFile)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var tenantId = User.FindFirstValue("TenantId");
            var existingSettings = await _context.Set<TenantSettings>()
                .FirstOrDefaultAsync(s => s.TenantId == tenantId);

            // Logo File Upload Logic
            if (logoFile != null && logoFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads/logos");
                Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = $"{tenantId}_{Guid.NewGuid()}_{Path.GetFileName(logoFile.FileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(fileStream);
                }

                model.LogoUrl = $"/uploads/logos/{uniqueFileName}";
            }
            else if (existingSettings != null)
            {
                model.LogoUrl = existingSettings.LogoUrl;
            }

            if (existingSettings == null)
            {
                model.TenantId = tenantId ?? string.Empty;
                _context.Add(model);
            }
            else
            {
                existingSettings.CompanyName = model.CompanyName;
                existingSettings.CompanyEmail = model.CompanyEmail;
                existingSettings.CompanyPhone = model.CompanyPhone;
                existingSettings.CompanyAddress = model.CompanyAddress;
                existingSettings.CurrencySymbol = model.CurrencySymbol;
                existingSettings.DefaultTaxRate = model.DefaultTaxRate;
                existingSettings.LogoUrl = model.LogoUrl;

                _context.Update(existingSettings);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "কোম্পানি সেটিংস সফলভাবে আপডেট করা হয়েছে।";

            return RedirectToAction(nameof(Index));
        }
    }
}