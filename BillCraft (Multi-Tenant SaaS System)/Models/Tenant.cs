using System;

namespace BillCraft.Web.Models
{
    public class Tenant : BaseEntity
    {
        public string TenantId { get; set; } = Guid.NewGuid().ToString();
        public string CompanyName { get; set; } = string.Empty;
        public string Subdomain { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";

        public int PlanId { get; set; }
        public SubscriptionPlan SubscriptionPlan { get; set; } = null!;
        public TenantSetting TenantSetting { get; set; } = null!;
    }
}