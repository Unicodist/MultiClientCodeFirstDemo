using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

namespace MultiTenantDemo.Client.Data.Migrations;

internal static class MigrationBuilderSqlExtensions
{
    public static OperationBuilder<SqlOperation> SqlDefinitionFromResource(this MigrationBuilder migrationBuilder, string relativePath)
    {
        var definition = EmbeddedSqlResource.Load(relativePath);
        return migrationBuilder.Sql($"EXEC (N'{definition.Replace("'", "''", StringComparison.Ordinal)}');");
    }
}
