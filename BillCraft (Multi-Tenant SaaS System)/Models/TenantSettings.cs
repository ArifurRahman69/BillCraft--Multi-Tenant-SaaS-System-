using System.ComponentModel.DataAnnotations;

namespace BillCraft__Multi_Tenant_SaaS_System_.Models
{
    public class TenantSettings
    {
        [Key]
        public int Id { get; set; }

        public string TenantId { get; set; } = string.Empty;

        [Required(ErrorMessage = "কোম্পানির নাম আবশ্যক")]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; } = string.Empty;

        [Display(Name = "Company Email")]
        [EmailAddress]
        public string? CompanyEmail { get; set; }

        [Display(Name = "Company Phone")]
        public string? CompanyPhone { get; set; }

        [Display(Name = "Company Address")]
        public string? CompanyAddress { get; set; }

        [Display(Name = "Company Logo URL")]
        public string? LogoUrl { get; set; }

        [Display(Name = "Currency Symbol")]
        public string CurrencySymbol { get; set; } = "৳";

        [Display(Name = "Default Tax Rate (%)")]
        public decimal DefaultTaxRate { get; set; } = 0m;
    }
}