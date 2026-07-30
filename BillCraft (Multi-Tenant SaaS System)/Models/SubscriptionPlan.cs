using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillCraft.Web.Models
{
    public class SubscriptionPlan : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PlanId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999.99)]
        public decimal Price { get; set; } = 0m;

        public int DurationInDays { get; set; } = 30;

        public int MaxUsersAllowed { get; set; } = 5;

        public int MaxInvoicesPerMonth { get; set; } = 100;

        public bool IsActive { get; set; } = true;

        public virtual ICollection<Tenant> Tenants { get; set; } = new List<Tenant>();
    }
}