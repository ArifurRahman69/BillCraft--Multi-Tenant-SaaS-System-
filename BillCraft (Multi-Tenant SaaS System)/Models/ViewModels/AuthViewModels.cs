using System.ComponentModel.DataAnnotations;

namespace BillCraft.Web.Models.ViewModels
{
    public class RegisterTenantViewModel
    {
        [Required(ErrorMessage = "কোম্পানির নাম দিন")]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "ইউনিক সাব-ডোমেন কি (Key) দিন")]
        [RegularExpression(@"^[a-zA-Z0-9-]+$", ErrorMessage = "শুধু অক্ষর, সংখ্যা ও হাইফেন (-) ব্যবহার করুন")]
        [Display(Name = "Subdomain Key (e.g. apex)")]
        public string Subdomain { get; set; } = string.Empty;

        [Required(ErrorMessage = "অ্যাডমিনের নাম দিন")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "ইমেইল দিন")]
        [EmailAddress(ErrorMessage = "সঠিক ইমেইল ফরম্যাট দিন")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "পাসওয়ার্ড দিন")]
        [MinLength(6, ErrorMessage = "কমপক্ষে ৬ অক্ষরের পাসওয়ার্ড দিন")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Compare("Password", ErrorMessage = "পাসওয়ার্ড দুটি মিলছে না")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "ইমেইল দিন")]
        [EmailAddress(ErrorMessage = "সঠিক ইমেইল দিন")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "পাসওয়ার্ড দিন")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }
    }
}