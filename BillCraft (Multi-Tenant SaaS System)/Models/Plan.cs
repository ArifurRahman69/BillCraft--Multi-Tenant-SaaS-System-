using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillCraft.Web.Models
{
    public class Plan
    {
        [Key]
        public int PlanId { get; set; }

        [Required(ErrorMessage = "প্ল্যানের নাম আবশ্যক")]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty; // e.g., Free, Standard, Pro

        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; } // 0.00 for Free

        public int DurationInDays { get; set; } = 30; // e.g., 30 Days, 365 Days

        // Subscription Limits
        public int MaxClients { get; set; } = 10; // -1 for Unlimited
        public int MaxInvoicesPerMonth { get; set; } = 20; // -1 for Unlimited
        public int MaxProducts { get; set; } = 10; // -1 for Unlimited

        public bool IsActive { get; set; } = true;
    }
}