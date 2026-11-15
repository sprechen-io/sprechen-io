// Erzeugt die eingecheckten TypeScript-Dateien für Navigationstexte,
// Telemetrie-Vokabular und Telemetrie-Schemadokumente aus den gemeinsamen Quellen.
using Sprechen.Forms.TypeScriptCodegen;

var repoRoot = args.Length >= 2 && args[0] == "--repo-root"
    ? args[1]
    : Directory.GetCurrentDirectory();

var outputPath = Path.Combine(repoRoot, FormsNavigationTextMapGenerator.OutputRelativePath);
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllText(outputPath, FormsNavigationTextMapGenerator.GenerateText());
Console.WriteLine($"Wrote {outputPath}");

var vocabularyPath = Path.Combine(repoRoot, TelemetryVocabularyGenerator.OutputRelativePath);
Directory.CreateDirectory(Path.GetDirectoryName(vocabularyPath)!);
File.WriteAllText(vocabularyPath, TelemetryVocabularyGenerator.GenerateText());
Console.WriteLine($"Wrote {vocabularyPath}");

var checkpointSchemaPath = Path.Combine(repoRoot, TelemetryCheckpointSchemaGenerator.OutputRelativePath);
Directory.CreateDirectory(Path.GetDirectoryName(checkpointSchemaPath)!);
File.WriteAllText(checkpointSchemaPath, TelemetryCheckpointSchemaGenerator.GenerateText(repoRoot));
Console.WriteLine($"Wrote {checkpointSchemaPath}");
