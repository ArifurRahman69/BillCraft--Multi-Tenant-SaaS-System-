using BillCraft.Web.Models; // আপনার প্রজেক্টের সঠিক Namespace দিন

namespace BillCraft__Multi_Tenant_SaaS_System_.Models
{
    public class GlobalSearchResultViewModel
    {
        public string Query { get; set; } = string.Empty;
        public List<Invoice> Invoices { get; set; } = new();
        public List<Client> Clients { get; set; } = new();
        public List<Product> Products { get; set; } = new();
    }
}