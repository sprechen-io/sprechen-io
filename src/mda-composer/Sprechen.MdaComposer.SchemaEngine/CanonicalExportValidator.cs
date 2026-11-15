using System.Text.Json;
using Json.Schema;
using Sprechen.Mda.SchemaBundle;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Prüft exportierte Dokumente gegen das kanonische Schema, damit ungültige Entwürfe vor dem Export erkannt werden.
/// </summary>
public sealed class CanonicalExportValidator
{
    private readonly SchemaBundleCache _schemaBundleCache = new();
    private readonly JsonSchemaRefusalCollector _refusalCollector = new();

    public CanonicalValidationResult Validate(
        string schemaFileName,
        string documentJson,
        IReadOnlyDictionary<string, string> schemaTextsByFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentJson);
        ArgumentNullException.ThrowIfNull(schemaTextsByFileName);

        var schemaBundle = _schemaBundleCache.GetOrBuild(schemaTextsByFileName);
        if (!schemaBundle.TryGetValue(schemaFileName, out var schema))
        {
            return new CanonicalValidationResult(
                IsValid: false,
                Issues:
                [
                    new CanonicalValidationIssue("$", $"Basisschema '{schemaFileName}' konnte nicht geladen werden.")
                ],
                Summary: $"Basisschema '{schemaFileName}' konnte nicht geladen werden.");
        }

        using var document = JsonDocument.Parse(documentJson);
        var evaluationResult = schema.Evaluate(
            document.RootElement,
            new EvaluationOptions
            {
                OutputFormat = OutputFormat.Hierarchical
            });

        var issues = _refusalCollector.Collect(evaluationResult)
            .Select(refusal => new CanonicalValidationIssue(refusal.InstanceLocation, refusal.Message))
            .ToArray();
        var summary = evaluationResult.IsValid
            ? "Export ist gegen das Basisschema valide."
            : _refusalCollector.Describe(evaluationResult);

        return new CanonicalValidationResult(evaluationResult.IsValid, issues, summary);
    }
}
