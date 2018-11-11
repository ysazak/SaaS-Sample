using Microsoft.AspNetCore.Http;

namespace saas_sample.Extensions
{
    public class TenantHttpContextAccessor : ITenantHttpContextAccessor, IHttpContextAccessor
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public TenantHttpContextAccessor(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public TenantContext TenantContext => httpContextAccessor.HttpContext?.GetTenantContext();
        public HttpContext HttpContext { get; set; }
    }
}