using System.Reflection;

namespace MultiTenantDemo.Client.Data.Migrations;

public static class EmbeddedSqlResource
{
    private const string Prefix = "MultiTenantDemo.Client.Data.Migrations.Sql.";

    public static string Load(string relativePath) => Load(typeof(EmbeddedSqlResource).Assembly, ToResourceName(relativePath));

    public static string ToResourceName(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        return Prefix + relativePath.Replace('\\', '/').Replace('/', '.');
    }

    internal static string Load(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded SQL resource '{resourceName}' was not found in {assembly.GetName().Name}. " +
                "Check the file exists and is included as <EmbeddedResource> in the .csproj.");

        using var reader = new StreamReader(stream);
        var sql = reader.ReadToEnd();
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new InvalidOperationException($"Embedded SQL resource '{resourceName}' is empty.");
        }

        return sql;
    }
}
