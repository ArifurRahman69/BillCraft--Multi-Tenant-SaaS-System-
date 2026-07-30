using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillCraft.Web.Models
{
    public class Tenant : BaseEntity
    {
        [Key]
        public string TenantId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(150)]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Subdomain { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Status { get; set; } = "Active";

        // Explicit Foreign Key Mapping
        public int PlanId { get; set; }

        [ForeignKey(nameof(PlanId))]
        public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;

        // Navigation Properties for 1-to-1 and 1-to-Many
        public virtual TenantSetting? TenantSetting { get; set; }

        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}