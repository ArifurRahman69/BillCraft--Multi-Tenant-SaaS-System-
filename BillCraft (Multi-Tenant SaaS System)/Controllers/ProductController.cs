using BillCraft.Web.Data;
using BillCraft.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BillCraft.Web.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Product
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }

        // GET: Product/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (!ModelState.IsValid) return View(product);

            var tenantId = User.FindFirst("TenantId")?.Value;
            if (!string.IsNullOrEmpty(tenantId))
            {
                product.TenantId = tenantId;
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "পণ্য/সেবা সফলভাবে যুক্ত করা হয়েছে!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Product/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null || !product.IsActive) return NotFound();

            return View(product);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.ProductId) return NotFound();

            if (!ModelState.IsValid) return View(product);

            try
            {
                var existingProduct = await _context.Products.FindAsync(id);
                if (existingProduct == null || !existingProduct.IsActive) return NotFound();

                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.UnitPrice = product.UnitPrice;
                existingProduct.Unit = product.Unit;

                _context.Update(existingProduct);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "পণ্য/সেবার তথ্য আপডেট করা হয়েছে!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Products.AnyAsync(e => e.ProductId == product.ProductId))
                    return NotFound();
                else
                    throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Product/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.IsActive = false; // Soft Delete
                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "পণ্য/সেবা সফলভাবে অপসারিত হয়েছে!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}