using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BillCraft.Web.Models
{
    public class InvoiceCreateViewModel
    {
        [Required(ErrorMessage = "কাস্টমার বা ক্লায়েন্ট সিলেক্ট করুন")]
        public int ClientId { get; set; }

        [Required(ErrorMessage = "ইনভয়েস নাম্বার দিন")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "ইস্যু তারিখ দিন")]
        public DateTime IssueDate { get; set; } = DateTime.Today;

        public DateTime? DueDate { get; set; } = DateTime.Today.AddDays(7);

        public decimal SubTotal { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "ট্যাক্স সঠিক দিন")]
        public decimal TaxAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "ডিসকাউন্ট সঠিক দিন")]
        public decimal DiscountAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public string? Notes { get; set; }

        // Dropdown Lists
        public List<SelectListItem> ClientList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ProductList { get; set; } = new List<SelectListItem>();

        // Dynamic Line Items
        public List<InvoiceItemViewModel> Items { get; set; } = new List<InvoiceItemViewModel>();
    }

    public class InvoiceItemViewModel
    {
        [Required(ErrorMessage = "আইটেম সিলেক্ট করুন")]
        public int? ProductId { get; set; }

        public string? Description { get; set; }

        // Decimal সরিয়ে Int করা হয়েছে যেন দশমিক না আসে
        [Range(1, int.MaxValue, ErrorMessage = "পরিমাণ কমপক্ষে ১ হতে হবে")]
        public int Quantity { get; set; } = 1;

        [Range(0, double.MaxValue, ErrorMessage = "একক মূল্য সঠিক দিন")]
        public decimal UnitPrice { get; set; }

        public decimal Amount { get; set; }
    }
}