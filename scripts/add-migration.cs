using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

if (args.Length != 2 || Target(args[0]) is not var (context, project))
{
    Console.Error.WriteLine("Usage: dotnet run scripts/add-migration.cs -- <client|system> <MigrationName>");
    return 1;
}

var name = args[1];
var root = RepositoryRoot();
var path = Path.Combine(root, "src", project);
var startup = Path.Combine(root, "src", "MultiTenantDemo.Api");

Dotnet(root, "restore", startup, "--verbosity", "quiet");

var output = Dotnet(root, "ef", "migrations", "add", name, "--context", context, "--project", path,
    "--startup-project", startup, "--json", "--prefix-output");

using var result = JsonDocument.Parse(string.Join('\n', output
    .Split('\n')
    .Where(line => line.StartsWith("data:", StringComparison.Ordinal))
    .Select(line => line["data:".Length..].TrimStart())));

foreach (var key in new[] { "migrationFile", "metadataFile", "snapshotFile" })
{
    var file = result.RootElement.GetProperty(key).GetString()!;
    var text = File.ReadAllText(file).TrimStart('﻿');
    File.WriteAllText(file, CommentLine().Replace(text, string.Empty), new UTF8Encoding(false));
    Console.WriteLine($"Created {Path.GetRelativePath(root, file).Replace('\\', '/')}");
}

return 0;

static (string Context, string Project)? Target(string kind) => kind switch
{
    "client" => ("ClientDbContext", "MultiTenantDemo.Client.Data"),
    "system" => ("SystemDbContext", "MultiTenantDemo.System.Data"),
    _ => null,
};

static string RepositoryRoot([CallerFilePath] string script = "") =>
    Path.GetFullPath(Path.Combine(Path.GetDirectoryName(script)!, ".."));

static string Dotnet(string workingDirectory, params string[] arguments)
{
    var start = new ProcessStartInfo("dotnet") { WorkingDirectory = workingDirectory, RedirectStandardOutput = true };
    foreach (var argument in arguments)
    {
        start.ArgumentList.Add(argument);
    }

    using var process = Process.Start(start)!;
    var output = process.StandardOutput.ReadToEnd();
    process.WaitForExit();
    if (process.ExitCode != 0)
    {
        Console.Error.Write(output);
        throw new InvalidOperationException($"dotnet {string.Join(' ', arguments)} failed with exit code {process.ExitCode}.");
    }

    return output.Replace("\r\n", "\n", StringComparison.Ordinal);
}

partial class Program
{
    [GeneratedRegex(@"^[ \t]*//[^\r\n]*(\r?\n)?", RegexOptions.Multiline)]
    private static partial Regex CommentLine();
}
