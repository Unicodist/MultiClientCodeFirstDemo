using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MultiTenantDemo.Api.Tenancy;
using MultiTenantDemo.Client.Data;
using MultiTenantDemo.IntegrationTests.Infrastructure;
using MultiTenantDemo.System.Domain;

namespace MultiTenantDemo.IntegrationTests;

public sealed class ComponentTests
{
    [Fact]
    public void Design_time_factory_never_targets_a_real_database_by_default()
    {
        Assert.Null(Environment.GetEnvironmentVariable(DesignTimeClientDbContextFactory.ConnectionEnvironmentVariable));
        using var db = new DesignTimeClientDbContextFactory().CreateDbContext([]);
        var connection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(db.Database.GetConnectionString());
        Assert.EndsWith(".invalid", connection.DataSource, StringComparison.Ordinal);
    }

    [Fact]
    public void TenantInfo_never_prints_its_connection_string()
    {
        var info = new TenantInfo("tenant-a", "server/db", "Server=server;Database=db;Password=hunter2");
        Assert.DoesNotContain("hunter2", info.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("hunter2", ActiveTenant("Password=hunter2").ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Server=other,1433;Database=t1;User ID=u;Password=p", "allowed server")]
    [InlineData("Server=sql01,1433;Database=master;User ID=u;Password=p", "database name")]
    [InlineData("Server=sql01,1433;Database=bad name;User ID=u;Password=p", "database name")]
    [InlineData("Server=sql01,1433;User ID=u;Password=p", "database name")]
    [InlineData("Server=sql01,1433;Database=t1;User ID=u;Password=p;TrustServerCertificate=True", "TrustServerCertificate")]
    [InlineData("this is not a connection string", "could not be parsed")]
    public void Invalid_connection_metadata_is_rejected(string connectionString, string reason)
    {
        var factory = Factory();
        var ex = Assert.Throws<TenantResolutionException>(() =>
            factory.Create(ActiveTenant(connectionString)));

        Assert.Equal(TenantResolutionFailure.InvalidConnectionMetadata, ex.Reason);
        Assert.Contains(reason, ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Password=p", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Valid_connection_metadata_produces_a_stable_database_key()
    {
        var info = Factory().Create(ActiveTenant("Server=SQL01,1433;Database=Tenant_A;User ID=u;Password=p"));
        Assert.Equal("sql01,1433/tenant_a", info.DatabaseKey);
    }

    private static Tenant ActiveTenant(string connectionString) =>
        new() { TenantId = "tenant-a", Name = "Tenant A", ConnectionString = connectionString, IsActive = true };

    private static TenantInfoFactory Factory()
    {
        var options = new TenantConnectionOptions();
        options.AllowedServers.Add("sql01,1433");
        return new TenantInfoFactory(Options.Create(options));
    }
}
