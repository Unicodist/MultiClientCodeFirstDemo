using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

var root = RepositoryRoot();
var configuration = Environment.GetEnvironmentVariable("CONFIGURATION") is { Length: > 0 } value ? value : "Debug";
var startup = Path.Combine(root, "src", "MultiTenantDemo.Api");
var build = !args.Contains("--no-build");

if (build)
{
    Dotnet(root, "build", startup, "--configuration", configuration, "--nologo", "--verbosity", "quiet");
}

Generate("ClientDbContext", "client", "MultiTenantDemo.Client.Data");
Generate("SystemDbContext", "system", "MultiTenantDemo.System.Data");
return 0;

void Generate(string context, string folder, string project)
{
    var directory = Path.Combine(root, "migrations", folder);
    var projectPath = Path.Combine(root, "src", project);

    string Ef(params string[] arguments) => Dotnet(root, [.. arguments, "--no-build", "--configuration", configuration,
        "--project", projectPath, "--startup-project", startup]);

    if (Directory.Exists(directory))
    {
        Directory.Delete(directory, recursive: true);
    }

    Directory.CreateDirectory(directory);

    var listing = Ef("ef", "migrations", "list", "--no-connect", "--json", "--prefix-output", "--context", context);
    using var json = JsonDocument.Parse(string.Join('\n', listing
        .Split('\n')
        .Where(line => line.StartsWith("data:", StringComparison.Ordinal))
        .Select(line => line["data:".Length..].TrimStart())));
    var migrations = json.RootElement.EnumerateArray().Select(m => m.GetProperty("id").GetString()!).ToList();

    var previous = "0";
    foreach (var migration in migrations)
    {
        Ef("ef", "migrations", "script", previous, migration, "--context", context, "--output", Path.Combine(directory, $"{migration}.up.sql"));
        Ef("ef", "migrations", "script", migration, previous, "--context", context, "--output", Path.Combine(directory, $"{migration}.down.sql"));
        previous = migration;
    }

    Ef("ef", "migrations", "script", "--idempotent", "--context", context, "--output", Path.Combine(directory, "idempotent.sql"));
    Console.WriteLine($"Generated {migrations.Count} migration(s) for {context} in migrations/{folder}");
}

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
