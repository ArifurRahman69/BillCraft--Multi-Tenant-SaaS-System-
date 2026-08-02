using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillCraft.Web.Models
{
    public class Invoice
    {
        [Key]
        public int InvoiceId { get; set; }

        [Required]
        public string TenantId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty; // e.g., INV-2026-001

        [Required]
        public int ClientId { get; set; }

        [ForeignKey("ClientId")]
        public virtual Client? Client { get; set; }

        [Required]
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;

        public DateTime? DueDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        // --- Record Payment Feature Extensions ---
        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; } = 0;

        [NotMapped]
        public decimal DueAmount => TotalAmount - PaidAmount;
        // ------------------------------------------

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Unpaid"; // Unpaid, Partially Paid, Paid, Overdue

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
    }
}