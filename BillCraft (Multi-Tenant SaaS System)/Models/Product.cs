using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillCraft.Web.Models
{
    public class Product : IMustHaveTenant
    {
        [Key]
        public int ProductId { get; set; }

        public string TenantId { get; set; } = string.Empty;

        [Required(ErrorMessage = "পণ্য বা সেবার নাম আবশ্যক")]
        [StringLength(150, ErrorMessage = "নাম ১৫০ অক্ষরের বেশি হতে পারবে না")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "একক মূল্য (Unit Price) আবশ্যক")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99, ErrorMessage = "মূল্য অবশ্যই ০ এর বেশি হতে হবে")]
        public decimal UnitPrice { get; set; }

        [Required(ErrorMessage = "একক (Unit) নির্বাচন বা টাইপ করুন")]
        [StringLength(50)]
        public string Unit { get; set; } = "Pcs"; // যেমন: Pcs, Hour, Month, Service, Item ইত্যাদি

        // Soft Delete-এর জন্য IsActive Flag
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property
        public virtual Tenant? Tenant { get; set; }
    }
}