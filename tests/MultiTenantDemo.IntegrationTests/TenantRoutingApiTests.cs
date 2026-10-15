using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MultiTenantDemo.Client.Domain;
using MultiTenantDemo.IntegrationTests.Infrastructure;
using MultiTenantDemo.System.Domain;
using MultiTenantDemo.System.Domain.Repositories;

namespace MultiTenantDemo.IntegrationTests;

[Collection(SqlServerTestGroup.Name)]
public sealed class TenantRoutingApiTests(SqlServerFixture sql) : IAsyncLifetime
{
    private TestTenant _tenantA = null!;
    private TestTenant _tenantB = null!;
    private TestTenant _inactive = null!;
    private TestTenant _misconfigured = null!;
    private string _systemDatabase = null!;

    public async Task InitializeAsync()
    {
        _tenantA = new TestTenant("tenant-a", await sql.CreateDatabaseAsync("apia"));
        _tenantB = new TestTenant("tenant-b", await sql.CreateDatabaseAsync("apib"));
        _inactive = new TestTenant("tenant-inactive", await sql.CreateDatabaseAsync("apiinactive"), IsActive: false);
        _misconfigured = new TestTenant("tenant-misconfigured", "master");

        foreach (var tenant in new[] { _tenantA, _tenantB, _inactive })
        {
            await ClientSchema.MigrateAsync(sql, tenant.DatabaseName);
        }

        _systemDatabase = await sql.CreateDatabaseAsync("system");
        await TenantConfiguration.SeedAsync(sql, _systemDatabase, [_tenantA, _tenantB, _inactive, _misconfigured]);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Tenants_resolve_to_different_databases()
    {
        await using var api = NewApi();

        var a = await ResolveAsync(api, _tenantA.TenantId);
        var b = await ResolveAsync(api, _tenantB.TenantId);

        Assert.EndsWith($"/{_tenantA.DatabaseName}", a.DatabaseKey, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith($"/{_tenantB.DatabaseName}", b.DatabaseKey, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(a.DatabaseKey, b.DatabaseKey);
        Assert.DoesNotContain(sql.Password, a.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Resolution_is_cached_for_the_request()
    {
        var counting = new CountingRegistry();
        await using var api = NewApi(services =>
        {
            var inner = services.Single(d => d.ServiceType == typeof(ITenantRepository));
            services.Replace(ServiceDescriptor.Singleton<ITenantRepository>(sp =>
            {
                counting.Inner = (ITenantRepository)ActivatorUtilities.CreateInstance(sp, inner.ImplementationType!);
                return counting;
            }));
        });

        using var scope = api.Services.CreateScope();
        SetRequest(scope, _tenantA.TenantId);
        var resolver = scope.ServiceProvider.GetRequiredService<ITenantResolver>();
        var first = await resolver.ResolveAsync();
        var second = await resolver.ResolveAsync();

        Assert.Same(first, second);
        Assert.Equal(1, counting.FindCalls);
    }

    [Fact]
    public async Task Data_written_for_tenant_A_is_only_visible_to_tenant_A()
    {
        await using var api = NewApi();
        using var client = api.CreateClient();

        var created = await Send(client, HttpMethod.Post, "/api/customers", _tenantA.TenantId,
            new CreateCustomerRequest { Name = "Ada", Email = "ada@example.com" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var forA = await (await Send(client, HttpMethod.Get, "/api/customers", _tenantA.TenantId)).Content.ReadFromJsonAsync<List<CustomerDto>>();
        var forB = await (await Send(client, HttpMethod.Get, "/api/customers", _tenantB.TenantId)).Content.ReadFromJsonAsync<List<CustomerDto>>();

        Assert.Equal("ada@example.com", Assert.Single(forA!).Email);
        Assert.Empty(forB!);

        Assert.Equal(1, await sql.ScalarAsync<int>("SELECT COUNT(*) FROM dbo.Customers", _tenantA.DatabaseName));
        Assert.Equal(0, await sql.ScalarAsync<int>("SELECT COUNT(*) FROM dbo.Customers", _tenantB.DatabaseName));

        var inB = await Send(client, HttpMethod.Post, "/api/customers", _tenantB.TenantId,
            new CreateCustomerRequest { Name = "Ada B", Email = "ada@example.com" });
        Assert.Equal(HttpStatusCode.Created, inB.StatusCode);

        var duplicateInA = await Send(client, HttpMethod.Post, "/api/customers", _tenantA.TenantId,
            new CreateCustomerRequest { Name = "Ada again", Email = "ADA@example.com" });
        Assert.Equal(HttpStatusCode.Conflict, duplicateInA.StatusCode);
    }

    [Fact]
    public async Task Stored_procedure_executes_and_maps_its_result()
    {
        await using var api = NewApi();
        using var client = api.CreateClient();
        await sql.ExecuteAsync(
            """
            INSERT dbo.Customers (Name, Email, IsActive, CreatedUtc) VALUES
              (N'Old', N'old@example.com', 0, '2020-01-01T00:00:00'),
              (N'New 1', N'new1@example.com', 1, '2026-05-01T10:00:00'),
              (N'New 2', N'new2@example.com', 1, '2026-06-01T12:30:00');
            """,
            _tenantB.DatabaseName);

        var summary = await (await Send(client, HttpMethod.Get, "/api/customers/summary?createdSinceUtc=2026-01-01T00:00:00Z", _tenantB.TenantId))
            .Content.ReadFromJsonAsync<CustomerSummaryDto>();

        Assert.Equal(new CustomerSummaryDto(3, 2, 2, new DateTime(2026, 6, 1, 12, 30, 0)), summary);

        var emptyTenant = await (await Send(client, HttpMethod.Get, "/api/customers/summary", _tenantA.TenantId))
            .Content.ReadFromJsonAsync<CustomerSummaryDto>();
        Assert.Equal(0, emptyTenant!.CreatedSince);
        Assert.Null(emptyTenant.LatestCreatedUtc);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.BadRequest)]
    [InlineData("", HttpStatusCode.BadRequest)]
    [InlineData("Tenant-A", HttpStatusCode.BadRequest)]
    [InlineData("../tenant-a", HttpStatusCode.BadRequest)]
    [InlineData("tenant-unknown", HttpStatusCode.NotFound)]
    [InlineData("tenant-inactive", HttpStatusCode.Forbidden)]
    [InlineData("tenant-misconfigured", HttpStatusCode.InternalServerError)]
    public async Task Invalid_tenant_requests_fail_closed(string? tenantId, HttpStatusCode expected)
    {
        await using var api = NewApi();
        using var client = api.CreateClient();

        var response = await Send(client, HttpMethod.Get, "/api/customers", tenantId);

        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        await AssertNoSecretsAsync(response);
    }

    [Fact]
    public async Task Registry_outage_returns_service_unavailable_without_falling_back()
    {
        await using var api = NewApi(services => services.Replace(ServiceDescriptor.Singleton<ITenantRepository, UnavailableRegistry>()));
        using var client = api.CreateClient();

        var response = await Send(client, HttpMethod.Get, "/api/customers", _tenantA.TenantId);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        await AssertNoSecretsAsync(response);
    }

    [Fact]
    public async Task Unreachable_system_database_returns_service_unavailable()
    {
        await using var api = new ApiFactory(sql, sql.NewDatabaseName("nosystem"));
        using var client = api.CreateClient();

        var response = await Send(client, HttpMethod.Get, "/api/customers", _tenantA.TenantId);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        await AssertNoSecretsAsync(response);
    }

    [Fact]
    public async Task Untrusted_connection_metadata_fails_closed()
    {
        await using var api = NewApi(services => services.Replace(ServiceDescriptor.Singleton<ITenantRepository>(
            new StaticRegistry(new Tenant
            {
                TenantId = _tenantA.TenantId,
                Name = _tenantA.TenantId,
                ConnectionString = $"Server=evil.example.com;Database={_tenantA.DatabaseName};User ID=sa;Password={sql.Password}",
                IsActive = true,
            }))));
        using var client = api.CreateClient();

        var response = await Send(client, HttpMethod.Get, "/api/customers", _tenantA.TenantId);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await AssertNoSecretsAsync(response);
    }

    [Fact]
    public async Task Api_logs_and_responses_never_contain_connection_secrets()
    {
        await using var api = NewApi();
        using var client = api.CreateClient();
        var responses = new List<HttpResponseMessage>
        {
            await Send(client, HttpMethod.Post, "/api/customers", _tenantA.TenantId, new CreateCustomerRequest { Name = "Log", Email = "log@example.com" }),
            await Send(client, HttpMethod.Get, "/api/customers", _tenantA.TenantId),
            await Send(client, HttpMethod.Get, "/api/customers/summary", _tenantA.TenantId),
            await Send(client, HttpMethod.Get, "/api/customers", "tenant-misconfigured"),
            await Send(client, HttpMethod.Get, "/api/customers", "tenant-unknown"),
        };

        foreach (var response in responses)
        {
            await AssertNoSecretsAsync(response);
        }

        var logs = string.Join('\n', api.Logs.GetSnapshot().Select(r => $"{r.Message} {r.Exception}"));
        Assert.NotEmpty(logs);
        Assert.DoesNotContain(sql.Password, logs, StringComparison.Ordinal);
        Assert.DoesNotContain("Password=", logs, StringComparison.OrdinalIgnoreCase);
    }

    private ApiFactory NewApi(Action<IServiceCollection>? configureServices = null) =>
        new(sql, _systemDatabase, configureServices);

    private static async Task<TenantInfo> ResolveAsync(ApiFactory api, string tenantId)
    {
        using var scope = api.Services.CreateScope();
        SetRequest(scope, tenantId);
        return await scope.ServiceProvider.GetRequiredService<ITenantResolver>().ResolveAsync();
    }

    private static void SetRequest(IServiceScope scope, string tenantId)
    {
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Request.Headers["X-Tenant-Id"] = tenantId;
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = context;
    }

    private static async Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string url, string? tenantId, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        if (tenantId is not null)
        {
            request.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        var response = await client.SendAsync(request);
        await response.Content.LoadIntoBufferAsync();
        return response;
    }

    private async Task AssertNoSecretsAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(sql.Password, body, StringComparison.Ordinal);
        Assert.DoesNotContain("Password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Server=", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mtd_", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(sql.DataSource, body, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CountingRegistry : ITenantRepository
    {
        private int _findCalls;

        public ITenantRepository Inner { get; set; } = null!;

        public int FindCalls => _findCalls;

        public Task<Tenant?> FindAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _findCalls);
            return Inner.FindAsync(tenantId, cancellationToken);
        }
    }

    private sealed class UnavailableRegistry : ITenantRepository
    {
        public Task<Tenant?> FindAsync(string tenantId, CancellationToken cancellationToken = default) =>
            throw new TenantRepositoryUnavailableException("Simulated outage.");
    }

    private sealed class StaticRegistry(params Tenant[] tenants) : ITenantRepository
    {
        public Task<Tenant?> FindAsync(string tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult(tenants.FirstOrDefault(t => t.TenantId == tenantId));
    }
}
