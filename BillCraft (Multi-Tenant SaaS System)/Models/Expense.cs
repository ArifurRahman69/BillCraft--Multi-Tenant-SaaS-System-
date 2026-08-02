using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillCraft.Web.Models
{
    public class Expense
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = string.Empty;

        [Required(ErrorMessage = "খরচের খাত নির্বাচন করুন")]
        public int ExpenseCategoryId { get; set; }

        [ForeignKey("ExpenseCategoryId")]
        public ExpenseCategory? Category { get; set; }

        [Required(ErrorMessage = "টাকার পরিমাণ প্রদান করুন")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 9999999.99, ErrorMessage = "সঠিক পরিমাণ লিখুন")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "তারিখ প্রদান করুন")]
        public DateTime ExpenseDate { get; set; } = DateTime.Now;

        [StringLength(255)]
        public string? Title { get; set; }

        public string? Description { get; set; }

        // রসিদের ছবি বা ডকুমেন্ট ফাইল পাথ
        public string? ReceiptFilePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}