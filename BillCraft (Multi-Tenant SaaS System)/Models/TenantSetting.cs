namespace BillCraft.Web.Models
{
    public class TenantSetting : IMustHaveTenant
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public string Currency { get; set; } = "BDT";
        public string CompanyAddress { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
    }
}