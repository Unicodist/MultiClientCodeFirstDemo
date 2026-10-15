using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace MultiTenantDemo.IntegrationTests.Infrastructure;

public sealed class SqlServerFixture : IAsyncLifetime
{
    public const string EnvironmentVariable = "MULTITENANT_TEST_SQLSERVER";

    private readonly ConcurrentBag<string> _databases = new();
    private MsSqlContainer? _container;

    public string ServerConnectionString { get; private set; } = string.Empty;

    public string DataSource { get; private set; } = string.Empty;

    public string Password { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configured))
        {
            _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
            await _container.StartAsync();
            configured = _container.GetConnectionString();
        }

        var builder = new SqlConnectionStringBuilder(configured)
        {
            InitialCatalog = string.Empty,
            TrustServerCertificate = true,
            Pooling = true,
        };
        builder.Remove("Database");
        ServerConnectionString = builder.ConnectionString;
        DataSource = builder.DataSource;
        Password = builder.Password;
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
            return;
        }

        SqlConnection.ClearAllPools();
        foreach (var name in _databases)
        {
            await ExecuteAsync(
                $"IF DB_ID(N'{name}') IS NOT NULL BEGIN ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]; END");
        }
    }

    public string NewDatabaseName(string label)
    {
        var name = $"mtd_{label}_{Guid.NewGuid().ToString("N")[..8]}";
        _databases.Add(name);
        return name;
    }

    public async Task<string> CreateDatabaseAsync(string label)
    {
        var name = NewDatabaseName(label);
        await ExecuteAsync($"CREATE DATABASE [{name}]");
        return name;
    }

    public string ConnectionStringFor(string database) =>
        new SqlConnectionStringBuilder(ServerConnectionString) { InitialCatalog = database }.ConnectionString;

    public async Task ExecuteAsync(string sql, string? database = null)
    {
        await using var connection = new SqlConnection(database is null ? ServerConnectionString : ConnectionStringFor(database));
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<T> ScalarAsync<T>(string sql, string database)
    {
        await using var connection = new SqlConnection(ConnectionStringFor(database));
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    public async Task<bool> DatabaseExistsAsync(string database)
    {
        await using var connection = new SqlConnection(ServerConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("SELECT CASE WHEN DB_ID(@name) IS NULL THEN 0 ELSE 1 END", connection);
        command.Parameters.AddWithValue("@name", database);
        return (int)(await command.ExecuteScalarAsync())! == 1;
    }
}

[CollectionDefinition(Name)]
public sealed class SqlServerTestGroup : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SQL Server";
}
