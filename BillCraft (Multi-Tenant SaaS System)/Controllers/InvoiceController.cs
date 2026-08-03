using BillCraft.Web.Data;
using BillCraft.Web.Models;
using BillCraft.Web.Services;
using BillCraft__Multi_Tenant_SaaS_System_.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BillCraft.Web.Controllers
{
    [Authorize]
    public class InvoiceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ISubscriptionService _subscriptionService;

        public InvoiceController(ApplicationDbContext context, ISubscriptionService subscriptionService)
        {
            _context = context;
            _subscriptionService = subscriptionService;
        }

        private string GetCurrentTenantId()
        {
            return User.FindFirstValue("TenantId") ?? "TENANT-001";
        }

        // GET: Invoice List
        public async Task<IActionResult> Index()
        {
            var tenantId = GetCurrentTenantId();

            var invoices = await _context.Invoices
                .Where(i => i.TenantId == tenantId)
                .Include(i => i.Client)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return View(invoices);
        }

        // GET: Invoice/Create
        public async Task<IActionResult> Create()
        {
            var tenantId = GetCurrentTenantId();

            // Tenant Settings থেকে Default Tax নিয়ে আসা
            var settings = await _context.Set<TenantSettings>()
                .FirstOrDefaultAsync(s => s.TenantId == tenantId);

            var viewModel = new InvoiceCreateViewModel
            {
                InvoiceNumber = "INV-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                ClientList = await GetClientSelectListAsync(tenantId),
                ProductList = await GetProductSelectListAsync(tenantId),
                TaxAmount = settings?.DefaultTaxRate ?? 0m,
                Items = new List<InvoiceItemViewModel> { new InvoiceItemViewModel() }
            };

            return View(viewModel);
        }

        // POST: Invoice/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InvoiceCreateViewModel model)
        {
            var tenantId = GetCurrentTenantId();

            // ১. সাবস্ক্রিপশন প্ল্যান লিমিট চেক
            bool canCreate = await _subscriptionService.CanCreateInvoiceAsync();
            if (!canCreate)
            {
                TempData["ErrorMessage"] = "চলতি মাসের জন্য আপনার ইনভয়েস তৈরির সীমা শেষ হয়ে গেছে! আনলিমিটেড ইনভয়েসের জন্য আপনার প্ল্যান আপগ্রেড করুন।";
                return RedirectToAction("Index", "Subscription");
            }

            if (model.Items == null || !model.Items.Any(i => i.ProductId.HasValue && i.ProductId > 0))
            {
                ModelState.AddModelError("", "কমপক্ষে একটি বৈধ প্রোডাক্ট বা সেবা যোগ করুন।");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    decimal calculatedSubTotal = model.Items.Sum(item => (decimal)item.Quantity * item.UnitPrice);
                    decimal calculatedTotal = calculatedSubTotal + model.TaxAmount - model.DiscountAmount;

                    var invoice = new Invoice
                    {
                        TenantId = tenantId,
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

            model.ClientList = await GetClientSelectListAsync(tenantId);
            model.ProductList = await GetProductSelectListAsync(tenantId);

            return View(model);
        }

        // GET: Invoice/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var tenantId = GetCurrentTenantId();

            var invoice = await _context.Invoices
                .Where(i => i.TenantId == tenantId)
                .Include(i => i.Client)
                .Include(i => i.InvoiceItems)
                .ThenInclude(item => item.Product)
                .FirstOrDefaultAsync(m => m.InvoiceId == id);

            if (invoice == null)
            {
                return NotFound();
            }

            ViewBag.TenantSettings = await _context.Set<TenantSettings>()
                .FirstOrDefaultAsync(s => s.TenantId == tenantId);

            return View(invoice);
        }

        // POST: Invoice/RecordPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordPayment(int invoiceId, decimal amount)
        {
            var tenantId = GetCurrentTenantId();
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.InvoiceId == invoiceId && i.TenantId == tenantId);

            if (invoice == null)
            {
                return NotFound();
            }

            if (amount <= 0)
            {
                TempData["ErrorMessage"] = "পেমেন্টের পরিমাণ ০ টাকার বেশি হতে হবে।";
                return RedirectToAction(nameof(Details), new { id = invoiceId });
            }

            decimal remainingAmount = invoice.TotalAmount - invoice.PaidAmount;
            if (amount > remainingAmount)
            {
                TempData["ErrorMessage"] = $"পেমেন্টের পরিমাণ বকেয়া টাকার ({remainingAmount:N2}) বেশি হতে পারবে না।";
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

            TempData["SuccessMessage"] = "পেমেন্ট সফলভাবে জমা নেওয়া হয়েছে!";
            return RedirectToAction(nameof(Details), new { id = invoiceId });
        }

        // GET: Download Invoice PDF
        [HttpGet]
        public async Task<IActionResult> DownloadPdf(int id)
        {
            var tenantId = GetCurrentTenantId();

            var invoice = await _context.Invoices
                .Where(i => i.TenantId == tenantId)
                .Include(i => i.Client)
                .Include(i => i.InvoiceItems)
                .ThenInclude(item => item.Product)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (invoice == null)
            {
                return NotFound();
            }

            var settings = await _context.Set<TenantSettings>()
                .FirstOrDefaultAsync(s => s.TenantId == tenantId);

            byte[] pdfBytes = GenerateInvoicePdfBytes(invoice, settings);

            string fileName = $"Invoice_{invoice.InvoiceNumber}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        // POST: Send Invoice via Email
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendEmail(int id)
        {
            var tenantId = GetCurrentTenantId();

            var invoice = await _context.Invoices
                .Where(i => i.TenantId == tenantId)
                .Include(i => i.Client)
                .Include(i => i.InvoiceItems)
                .ThenInclude(item => item.Product)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (invoice == null || string.IsNullOrEmpty(invoice.Client?.Email))
            {
                TempData["ErrorMessage"] = "ক্লায়েন্ট অথবা ইমেইল এড্রেস পাওয়া যায়নি।";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var settings = await _context.Set<TenantSettings>()
                    .FirstOrDefaultAsync(s => s.TenantId == tenantId);

                byte[] pdfBytes = GenerateInvoicePdfBytes(invoice, settings);

                string companyName = settings?.CompanyName ?? "BillCraft";
                string currency = settings?.CurrencySymbol ?? "৳";

                string subject = $"Invoice #{invoice.InvoiceNumber} from {companyName}";
                string body = $@"
                    <h3>প্রিয় {invoice.Client.Name},</h3>
                    <p>আপনার সাম্প্রতিক ইনভয়েস <strong>#{invoice.InvoiceNumber}</strong> তৈরি করা হয়েছে।</p>
                    <p><strong>মোট পরিমাণ:</strong> {currency}{invoice.TotalAmount:N2}<br/>
                    <strong>পরিশোধের শেষ তারিখ:</strong> {invoice.DueDate:dd MMM yyyy}</p>
                    <p>বিস্তারিত দেখতে সংযুক্ত PDF ফাইলটি দেখুন।</p>
                    <br/>
                    <p>ধন্যবাদ,<br/>{companyName}</p>";

                // TODO: Un-comment when EmailService is configured
                // await _emailService.SendEmailWithAttachmentAsync(invoice.Client.Email, subject, body, pdfBytes, $"Invoice_{invoice.InvoiceNumber}.pdf");

                TempData["SuccessMessage"] = $"ইনভয়েসটি সফলভাবে {invoice.Client.Email} ঠিকানায় পাঠানো হয়েছে।";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "ইমেইল পাঠাতে সমস্যা হয়েছে। অনুগ্রহ করে পরে চেষ্টা করুন।";
            }

            return RedirectToAction(nameof(Index));
        }

        // API to Get Product Details
        [HttpGet]
        public async Task<IActionResult> GetProductDetails(int id)
        {
            var tenantId = GetCurrentTenantId();
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == id && p.TenantId == tenantId);

            if (product == null) return NotFound();

            return Json(new { price = product.UnitPrice, description = product.Name });
        }

        #region Helper Methods
        private byte[] GenerateInvoicePdfBytes(Invoice invoice, TenantSettings? settings)
        {
            string companyName = settings?.CompanyName ?? "BillCraft Solutions";
            string companyEmail = settings?.CompanyEmail ?? "";
            string companyPhone = settings?.CompanyPhone ?? "";
            string companyAddress = settings?.CompanyAddress ?? "";
            string currency = settings?.CurrencySymbol ?? "৳";

            string logoImgHtml = "";
            if (!string.IsNullOrEmpty(settings?.LogoUrl))
            {
                try
                {
                    string webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    string fullLogoPath = Path.Combine(webRootPath, settings.LogoUrl.TrimStart('/', '\\'));

                    if (System.IO.File.Exists(fullLogoPath))
                    {
                        byte[] imageArray = System.IO.File.ReadAllBytes(fullLogoPath);
                        string base64Image = Convert.ToBase64String(imageArray);
                        string ext = Path.GetExtension(fullLogoPath).Replace(".", "").ToLower();
                        logoImgHtml = $"<img src='data:image/{ext};base64,{base64Image}' style='max-height: 60px; margin-bottom: 10px;' /><br/>";
                    }
                }
                catch
                {
                    // Fallback cleanly if file fails
                }
            }

            using (MemoryStream ms = new MemoryStream())
            {
                using (var writer = new StreamWriter(ms))
                {
                    writer.Write($@"
                        <html>
                        <head>
                            <meta charset='utf-8' />
                            <style>
                                body {{ font-family: Arial, sans-serif; padding: 20px; color: #333; }}
                                .header {{ display: flex; justify-content: space-between; border-bottom: 2px solid #6366f1; padding-bottom: 10px; margin-bottom: 20px; }}
                                .table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
                                .table th, .table td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
                                .table th {{ background-color: #f4f5f7; }}
                                .total-box {{ text-align: right; margin-top: 20px; font-size: 15px; }}
                            </style>
                        </head>
                        <body>
                            <div class='header'>
                                <div>
                                    {logoImgHtml}
                                    <h2 style='margin:0;'>{companyName}</h2>
                                    <p style='margin:5px 0;'>{companyAddress}<br/>Email: {companyEmail} | Phone: {companyPhone}</p>
                                </div>
                                <div style='text-align: right;'>
                                    <h3 style='color: #6366f1; margin:0;'>INVOICE</h3>
                                    <p style='margin:5px 0;'><strong>#{invoice.InvoiceNumber}</strong><br/>Date: {invoice.IssueDate:dd MMM yyyy}</p>
                                </div>
                            </div>
                            <div style='margin-bottom: 20px;'>
                                <strong>Bill To:</strong><br/>
                                {invoice.Client?.Name}<br/>
                                {invoice.Client?.Email}<br/>
                                {invoice.Client?.Phone}
                            </div>
                            <table class='table'>
                                <thead>
                                    <tr>
                                        <th>Item Description</th>
                                        <th>Qty</th>
                                        <th>Unit Price</th>
                                        <th>Total</th>
                                    </tr>
                                </thead>
                                <tbody>");

                    foreach (var item in invoice.InvoiceItems)
                    {
                        writer.Write($@"
                                    <tr>
                                        <td>{(item.Product != null ? item.Product.Name : item.Description)}</td>
                                        <td>{item.Quantity}</td>
                                        <td>{currency}{item.UnitPrice:N2}</td>
                                        <td>{currency}{item.Amount:N2}</td>
                                    </tr>");
                    }

                    writer.Write($@"
                                </tbody>
                            </table>
                            <div class='total-box'>
                                <p><strong>Subtotal:</strong> {currency}{invoice.SubTotal:N2}</p>
                                <p><strong>Tax:</strong> {currency}{invoice.TaxAmount:N2}</p>
                                <p><strong>Discount:</strong> {currency}{invoice.DiscountAmount:N2}</p>
                                <p style='font-size: 18px;'><strong>Total Amount:</strong> {currency}{invoice.TotalAmount:N2}</p>
                                <p><strong>Paid Amount:</strong> {currency}{invoice.PaidAmount:N2}</p>
                                <p style='color: red;'><strong>Due Amount:</strong> {currency}{(invoice.TotalAmount - invoice.PaidAmount):N2}</p>
                            </div>
                        </body>
                        </html>");

                    writer.Flush();
                }
                return ms.ToArray();
            }
        }

        private async Task<List<SelectListItem>> GetClientSelectListAsync(string tenantId)
        {
            return await _context.Clients
                .Where(c => c.TenantId == tenantId)
                .Select(c => new SelectListItem
                {
                    Value = c.ClientId.ToString(),
                    Text = c.Name + " (" + c.Phone + ")"
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> GetProductSelectListAsync(string tenantId)
        {
            return await _context.Products
                .Where(p => p.TenantId == tenantId)
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