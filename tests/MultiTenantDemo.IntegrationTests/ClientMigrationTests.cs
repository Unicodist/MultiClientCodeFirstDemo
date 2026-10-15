using MultiTenantDemo.IntegrationTests.Infrastructure;

namespace MultiTenantDemo.IntegrationTests;

[Collection(SqlServerTestGroup.Name)]
public sealed class ClientMigrationTests(SqlServerFixture sql)
{
    private const string ProcedureParameters =
        "SELECT COUNT(*) FROM sys.parameters WHERE object_id = OBJECT_ID(N'dbo.GetCustomerSummary')";

    private const string ProcedureExists =
        "SELECT CASE WHEN OBJECT_ID(N'dbo.GetCustomerSummary', N'P') IS NULL THEN 0 ELSE 1 END";

    private const string CustomersExists =
        "SELECT CASE WHEN OBJECT_ID(N'dbo.Customers', N'U') IS NULL THEN 0 ELSE 1 END";

    [Fact]
    public async Task Stored_procedure_can_be_reverted_to_each_earlier_version_and_reapplied()
    {
        var database = await sql.CreateDatabaseAsync("migrations");

        await ClientSchema.MigrateAsync(sql, database);
        Assert.Equal(1, await sql.ScalarAsync<int>(ProcedureParameters, database));

        await ClientSchema.MigrateAsync(sql, database, "AddGetCustomerSummaryProcedure");
        Assert.Equal(1, await sql.ScalarAsync<int>(ProcedureExists, database));
        Assert.Equal(0, await sql.ScalarAsync<int>(ProcedureParameters, database));
        Assert.Equal(0L, await sql.ScalarAsync<long>("EXEC dbo.GetCustomerSummary", database));

        await ClientSchema.MigrateAsync(sql, database, "InitialCreate");
        Assert.Equal(0, await sql.ScalarAsync<int>(ProcedureExists, database));
        Assert.Equal(1, await sql.ScalarAsync<int>(CustomersExists, database));

        await ClientSchema.MigrateAsync(sql, database, "0");
        Assert.Equal(0, await sql.ScalarAsync<int>(CustomersExists, database));
        Assert.Empty(await ClientSchema.AppliedMigrationsAsync(sql, database));

        await ClientSchema.MigrateAsync(sql, database);
        Assert.Equal(1, await sql.ScalarAsync<int>(ProcedureParameters, database));
        Assert.Equal(3, (await ClientSchema.AppliedMigrationsAsync(sql, database)).Length);
    }
}
