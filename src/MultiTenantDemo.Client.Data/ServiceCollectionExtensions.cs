using Microsoft.Extensions.DependencyInjection;
using MultiTenantDemo.Client.Data.Repositories;
using MultiTenantDemo.Client.Domain.Repositories;

namespace MultiTenantDemo.Client.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClientData<TConnectionStringProvider>(this IServiceCollection services)
        where TConnectionStringProvider : class, IClientConnectionStringProvider
    {
        services.AddScoped<IClientConnectionStringProvider, TConnectionStringProvider>();
        services.AddScoped<IClientDbContextFactory, ClientDbContextFactory>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        return services;
    }
}
