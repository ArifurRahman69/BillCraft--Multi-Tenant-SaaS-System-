using System.ComponentModel.DataAnnotations;

namespace BillCraft.Web.Models
{
    public class Client : IMustHaveTenant
    {
        [Key]
        public int ClientId { get; set; }

        public string TenantId { get; set; } = string.Empty;

        [Required(ErrorMessage = "ক্লায়েন্টের নাম আবশ্যক")]
        [StringLength(100, ErrorMessage = "নাম ১০০ অক্ষরের বেশি হতে পারবে না")]
        public string Name { get; set; } = string.Empty;

        // Email Validation (@gmail.com বাধ্যতামূলক যদি ইনপুট দেওয়া হয়)
        [EmailAddress(ErrorMessage = "সঠিক ইমেইল ফরম্যাট দিন")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@gmail\.com$", ErrorMessage = "ইমেইলটি অবশ্যই একটি বৈধ @gmail.com ঠিকানা হতে হবে")]
        public string? Email { get; set; }

        // Phone Number Validation (ঠিক ১১ ডিজিটের বিডি ফোন নম্বর)
        [Required(ErrorMessage = "ফোন নম্বর আবশ্যক")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "ফোন নম্বরটি অবশ্যই ঠিক ১১ ডিজিটের হতে হবে")]
        [RegularExpression(@"^01[3-9]\d{8}$", ErrorMessage = "সঠিক বাংলাদেশী ফোন নম্বর দিন (যেমন: 01712345678)")]
        public string Phone { get; set; } = string.Empty;

        public string? CompanyName { get; set; }

        public string? Address { get; set; }

        // Soft Delete-এর জন্য IsActive Flag
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property
        public virtual Tenant? Tenant { get; set; }
    }
}