using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace BillCraft.Web.Data
{
    public interface ITenantProvider
    {
        string GetTenantId();
    }

    public class TenantProvider : ITenantProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetTenantId()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            // Ensure the user is authenticated before checking claims
            if (user?.Identity != null && user.Identity.IsAuthenticated)
            {
                var tenantId = user.FindFirstValue("TenantId");
                if (!string.IsNullOrEmpty(tenantId))
                {
                    return tenantId;
                }
            }

            return string.Empty;
        }
    }
}