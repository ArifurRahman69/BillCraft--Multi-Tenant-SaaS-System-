using System.ComponentModel.DataAnnotations;

namespace BillCraft.Web.Models
{
    public class TenantSubscription : IMustHaveTenant
    {
        [Key]
        public int SubscriptionId { get; set; }

        public string TenantId { get; set; } = string.Empty;

        public int PlanId { get; set; }

        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Plan? Plan { get; set; }
        public virtual Tenant? Tenant { get; set; }
    }
}