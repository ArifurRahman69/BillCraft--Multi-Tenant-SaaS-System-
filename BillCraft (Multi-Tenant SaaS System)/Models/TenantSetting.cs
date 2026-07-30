using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillCraft.Web.Models
{
    public class TenantSetting : IMustHaveTenant
    {
        [Key, ForeignKey(nameof(Tenant))]
        public string TenantId { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "BDT";

        [Required]
        [MaxLength(250)]
        public string CompanyAddress { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        public virtual Tenant Tenant { get; set; } = null!;
    }
}