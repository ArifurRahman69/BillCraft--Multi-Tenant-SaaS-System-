using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BillCraft.Web.Data;
using BillCraft.Web.Models;

namespace BillCraft.Web.Controllers
{
    public class InvoiceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InvoiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Invoice List
        public async Task<IActionResult> Index()
        {
            var invoices = await _context.Invoices
                .Include(i => i.Client)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return View(invoices);
        }

        // GET: Invoice/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new InvoiceCreateViewModel
            {
                InvoiceNumber = "INV-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                ClientList = await GetClientSelectListAsync(),
                ProductList = await GetProductSelectListAsync(),
                Items = new List<InvoiceItemViewModel> { new InvoiceItemViewModel() }
            };

            return View(viewModel);
        }

        // POST: Invoice/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InvoiceCreateViewModel model)
        {
            if (model.Items == null || !model.Items.Any())
            {
                ModelState.AddModelError("", "কমপক্ষে একটি প্রোডাক্ট বা সেবা যোগ করুন।");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    decimal calculatedSubTotal = model.Items.Sum(item => (decimal)item.Quantity * item.UnitPrice);
                    decimal calculatedTotal = calculatedSubTotal + model.TaxAmount - model.DiscountAmount;

                    var invoice = new Invoice
                    {
                        TenantId = "TENANT-001",
                        InvoiceNumber = model.InvoiceNumber,
                        ClientId = model.ClientId,
                        IssueDate = model.IssueDate,
                        DueDate = model.DueDate,
                        SubTotal = calculatedSubTotal,
                        TaxAmount = model.TaxAmount,
                        DiscountAmount = model.DiscountAmount,
                        TotalAmount = calculatedTotal > 0 ? calculatedTotal : 0,
                        PaidAmount = 0,
                        Notes = model.Notes,
                        Status = "Unpaid",
                        CreatedAt = DateTime.UtcNow
                    };

                    foreach (var item in model.Items)
                    {
                        if (item.ProductId.HasValue && item.ProductId > 0)
                        {
                            invoice.InvoiceItems.Add(new InvoiceItem
                            {
                                ProductId = item.ProductId.Value,
                                Description = item.Description ?? string.Empty,
                                Quantity = item.Quantity,
                                UnitPrice = item.UnitPrice,
                                Amount = (decimal)item.Quantity * item.UnitPrice
                            });
                        }
                    }

                    _context.Invoices.Add(invoice);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "ইনভয়েস সফলভাবে তৈরি করা হয়েছে!";
                    return RedirectToAction(nameof(Details), new { id = invoice.InvoiceId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "ইনভয়েস সেভ করার সময় ত্রুটি ঘটেছে: " + ex.Message);
                }
            }

            model.ClientList = await GetClientSelectListAsync();
            model.ProductList = await GetProductSelectListAsync();

            return View(model);
        }

        // GET: Invoice/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.InvoiceItems)
                .ThenInclude(item => item.Product)
                .FirstOrDefaultAsync(m => m.InvoiceId == id);

            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }

        // POST: Invoice/RecordPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordPayment(int invoiceId, decimal amount)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);

            if (invoice == null)
            {
                return NotFound();
            }

            if (amount <= 0)
            {
                TempData["ErrorMessage"] = "পেমেন্টের পরিমাণ ০ টাকার বেশি হতে হবে।";
                return RedirectToAction(nameof(Details), new { id = invoiceId });
            }

            // Record Payment
            invoice.PaidAmount += amount;

            // Auto-update Status
            if (invoice.PaidAmount >= invoice.TotalAmount)
            {
                invoice.Status = "Paid";
            }
            else if (invoice.PaidAmount > 0)
            {
                invoice.Status = "Partially Paid";
            }

            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "পেমেন্ট সফলভাবে জমা নেওয়া হয়েছে!";
            return RedirectToAction(nameof(Details), new { id = invoiceId });
        }

        // API to Get Product Details
        [HttpGet]
        public async Task<IActionResult> GetProductDetails(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            return Json(new { price = product.UnitPrice, description = product.Name });
        }

        #region Helper Methods
        private async Task<List<SelectListItem>> GetClientSelectListAsync()
        {
            return await _context.Clients
                .Select(c => new SelectListItem
                {
                    Value = c.ClientId.ToString(),
                    Text = c.Name + " (" + c.Phone + ")"
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> GetProductSelectListAsync()
        {
            return await _context.Products
                .Select(p => new SelectListItem
                {
                    Value = p.ProductId.ToString(),
                    Text = p.Name + " - ৳" + p.UnitPrice
                })
                .ToListAsync();
        }
        #endregion
    }
}