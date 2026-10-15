using System.Text.RegularExpressions;

namespace MultiTenantDemo.System.Domain;

public static partial class TenantId
{
    public const int MaxLength = 64;

    public static bool IsValid(string? value) =>
        !string.IsNullOrEmpty(value) && value.Length <= MaxLength && Pattern().IsMatch(value);

    [GeneratedRegex("^[a-z0-9][a-z0-9-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
