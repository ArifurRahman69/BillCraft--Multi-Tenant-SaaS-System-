namespace BillCraft.Web.Models
{
    public class User : BaseEntity, IMustHaveTenant
    {
        public int UserId { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "TenantAdmin";
        public bool IsActive { get; set; } = true;
    }
}