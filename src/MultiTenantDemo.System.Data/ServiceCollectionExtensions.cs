using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MultiTenantDemo.System.Data.Repositories;
using MultiTenantDemo.System.Domain.Repositories;

namespace MultiTenantDemo.System.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSystemData(this IServiceCollection services)
    {
        services.TryAddSingleton<ISystemDbContextFactory, SystemDbContextFactory>();
        services.TryAddSingleton<ITenantRepository, TenantRepository>();
        return services;
    }
}
