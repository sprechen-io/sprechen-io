using System.Text.Json;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft nicht leere Texte für einen Sprachschlüssel. Fehlende optionale Wörterbücher
/// werden übersprungen. Die JSON-Variante verlangt ein Objekt mit entsprechendem Meldungstext.
/// </summary>
internal static class FormLocalizedTextCoverageRule
{
    internal static void Validate(
        IReadOnlyDictionary<string, string>? text,
        string language,
        string path,
        string context,
        List<PreflightIssue> issues,
        string? id = null,
        string code = PreflightCodes.ErrorLocalizationMissing)
    {
        if (text is null)
        {
            return;
        }

        if (text.TryGetValue(language, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        AddMissingLocalization(language, path, context, issues, id, code);
    }

    internal static void Validate(
        JsonElement? textElement,
        string language,
        string path,
        string context,
        List<PreflightIssue> issues,
        string? id = null,
        string code = PreflightCodes.ErrorLocalizationMissing)
    {
        if (textElement is not { ValueKind: JsonValueKind.Object })
        {
            AddMissingLocalization(language, path, context, issues, id, code);
            return;
        }

        if (PreflightJsonValueReader.TryGetLocalizedTextValue(textElement.Value, "message", language, out var value)
            && !string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        AddMissingLocalization(language, path, context, issues, id, code);
    }

    private static void AddMissingLocalization(
        string language,
        string path,
        string context,
        List<PreflightIssue> issues,
        string? id,
        string code)
    {
        issues.Add(new PreflightIssue(
            Code: code,
            Message: $"Missing localization for language '{language}' in {context}.",
            Path: path,
            Id: id));
    }
}
