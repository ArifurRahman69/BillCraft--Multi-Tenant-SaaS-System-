using BillCraft.Web.Data;
using BillCraft.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Security.Claims;

namespace BillCraft.Web.Controllers
{
    [Authorize]
    public class ExpenseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ExpenseController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        private string GetCurrentTenantId()
        {
            return User.FindFirstValue("TenantId") ?? string.Empty;
        }

        // GET: Expense List
        public async Task<IActionResult> Index()
        {
            var tenantId = GetCurrentTenantId();

            var expenses = await _context.Expenses
                .Where(e => e.TenantId == tenantId)
                .Include(e => e.Category)
                .OrderByDescending(e => e.ExpenseDate)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.TotalExpenses = expenses.Sum(e => e.Amount);
            return View(expenses);
        }

        // GET: Expense/Create
        public async Task<IActionResult> Create()
        {
            var tenantId = GetCurrentTenantId();
            ViewBag.Categories = await _context.ExpenseCategories
                .Where(c => c.TenantId == tenantId && c.IsActive)
                .ToListAsync();

            return View();
        }

        // POST: Expense/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Expense expense, IFormFile? receiptFile)
        {
            var tenantId = GetCurrentTenantId();

            if (ModelState.IsValid)
            {
                expense.TenantId = tenantId;
                expense.CreatedAt = DateTime.UtcNow;

                // Receipt File Upload Handling
                if (receiptFile != null && receiptFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "receipts");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(receiptFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await receiptFile.CopyToAsync(fileStream);
                    }

                    expense.ReceiptFilePath = "/uploads/receipts/" + uniqueFileName;
                }

                _context.Add(expense);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "খরচ সফলভাবে এন্ট্রি করা হয়েছে!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = await _context.ExpenseCategories
                .Where(c => c.TenantId == tenantId && c.IsActive)
                .ToListAsync();

            return View(expense);
        }

        // GET: Expense Categories List & Quick Add
        public async Task<IActionResult> Categories()
        {
            var tenantId = GetCurrentTenantId();
            var categories = await _context.ExpenseCategories
                .Where(c => c.TenantId == tenantId)
                .AsNoTracking()
                .ToListAsync();

            return View(categories);
        }

        // POST: Add Category
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(ExpenseCategory category)
        {
            if (ModelState.IsValid)
            {
                var tenantId = GetCurrentTenantId();
                category.TenantId = tenantId;

                _context.ExpenseCategories.Add(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "ক্যাটাগরি সফলভাবে যুক্ত করা হয়েছে!";
            }
            return RedirectToAction(nameof(Categories));
        }

        // POST: Delete Expense
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var tenantId = GetCurrentTenantId();
            var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

            if (expense != null)
            {
                // Delete physical receipt file if exists
                if (!string.IsNullOrEmpty(expense.ReceiptFilePath))
                {
                    string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, expense.ReceiptFilePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }

                _context.Expenses.Remove(expense);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "খরচের রেকর্ড মুছে ফেলা হয়েছে।";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}