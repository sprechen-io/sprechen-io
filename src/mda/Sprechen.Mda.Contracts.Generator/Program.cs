using Sprechen.Mda.Contracts.Generator;

var verifyOnly = args.Any(argument => string.Equals(argument, "--verify", StringComparison.Ordinal));
var repoRootArgument = args
    .Select((argument, index) => (argument, index))
    .FirstOrDefault(x => string.Equals(x.argument, "--repo-root", StringComparison.Ordinal));
var repoRoot = repoRootArgument.argument is null || repoRootArgument.index + 1 >= args.Length
    ? null
    : args[repoRootArgument.index + 1];
var resolvedBaseDirectory = string.IsNullOrWhiteSpace(repoRoot)
    ? Directory.GetCurrentDirectory()
    : Path.GetFullPath(repoRoot);

if (!verifyOnly)
{
    SchemaContractGenerator.WriteArtifacts(repoRoot);
    return;
}

var generatedOutputs = SchemaContractGenerator.GenerateArtifacts(repoRoot);
var mismatches = new List<string>();
foreach (var output in generatedOutputs)
{
    var absolutePath = Path.Combine(resolvedBaseDirectory, output.Key);
    if (!File.Exists(absolutePath))
    {
        mismatches.Add($"{output.Key} (missing)");
        continue;
    }

    var current = File.ReadAllText(absolutePath);
    if (!string.Equals(Normalize(current), Normalize(output.Value), StringComparison.Ordinal))
    {
        mismatches.Add(output.Key);
    }
}

if (mismatches.Count == 0)
{
    return;
}

Console.Error.WriteLine("Generated schema contracts are out of date:");
foreach (var mismatch in mismatches)
{
    Console.Error.WriteLine($"- {mismatch}");
}

Environment.ExitCode = 1;

static string Normalize(string value)
    => value.Replace("\r\n", "\n", StringComparison.Ordinal);
