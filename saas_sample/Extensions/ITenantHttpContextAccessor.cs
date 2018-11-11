namespace saas_sample.Extensions
{
    public interface ITenantHttpContextAccessor
    {
        TenantContext TenantContext { get; }
    }
}