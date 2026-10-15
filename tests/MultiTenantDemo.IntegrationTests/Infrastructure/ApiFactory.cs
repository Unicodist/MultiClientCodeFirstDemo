using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace MultiTenantDemo.IntegrationTests.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly Dictionary<string, string?> _settings;
    private readonly Action<IServiceCollection>? _configureServices;

    public ApiFactory(SqlServerFixture sql, string systemDatabase, Action<IServiceCollection>? configureServices = null)
    {
        _settings = TenantConfiguration.For(sql, systemDatabase);

        _settings["Security:AcknowledgeUnauthenticatedTenantHeader"] = "true";
        _configureServices = configureServices;
    }

    public FakeLogCollector Logs => Services.GetFakeLogCollector();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTest");
        foreach (var (key, value) in _settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Debug).AddFakeLogging());
        if (_configureServices is not null)
        {
            builder.ConfigureServices(_configureServices);
        }
    }
}
