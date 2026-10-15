using Microsoft.Extensions.DependencyInjection.Extensions;
using MultiTenantDemo.Api.Tenancy;
using MultiTenantDemo.Client.Data;
using MultiTenantDemo.Client.Domain;
using MultiTenantDemo.Client.Domain.Services;
using MultiTenantDemo.System.Data;
using MultiTenantDemo.System.Domain;

namespace MultiTenantDemo.Api;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTenancy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SystemDatabaseOptions>()
            .Bind(configuration.GetSection(SystemDatabaseOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString), $"{SystemDatabaseOptions.SectionName}:ConnectionString is required.")
            .ValidateOnStart();

        services.AddOptions<TenantConnectionOptions>()
            .Bind(configuration.GetSection(TenantConnectionOptions.SectionName))
            .Validate(o => o.AllowedServers.Count > 0, $"{TenantConnectionOptions.SectionName}:AllowedServers must list at least one server.")
            .ValidateOnStart();

        services.AddHttpContextAccessor();
        services.AddSingleton<TenantInfoFactory>();
        services.AddScoped<ITenantResolver, TenantResolver>();

        services.AddSystemData();
        services.AddClientData<TenantConnectionStringProvider>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ICustomerService, CustomerService>();
        return services;
    }
}
