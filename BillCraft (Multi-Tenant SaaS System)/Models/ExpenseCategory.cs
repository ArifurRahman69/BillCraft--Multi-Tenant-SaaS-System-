using System.ComponentModel.DataAnnotations;

namespace BillCraft.Web.Models
{
    public class ExpenseCategory
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = string.Empty;

        [Required(ErrorMessage = "ক্যাটাগরির নাম আবশ্যক")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}