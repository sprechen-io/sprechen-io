using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft den eingebetteten Auswahldialog und dessen Unterstützung
/// für die Sprachen der Anwendungen.
/// </summary>
internal sealed class CatalogSelectionDialogPreflightValidator
{
    public void Validate(
        List<PreflightIssue> errors,
        List<PreflightIssue> warnings,
        FormsCatalog catalog,
        IReadOnlyList<string> supportedLanguages,
        string path = "$.selectionDialog")
    {
        var selectionDialog = catalog.SelectionDialog;
        var selectionDialogPreflight = ApplicationSelectionDialogPreflight.Validate(selectionDialog);
        errors.AddRange(selectionDialogPreflight.Errors.Select(issue => PrefixIssuePath(issue, path)));
        warnings.AddRange(selectionDialogPreflight.Warnings.Select(issue => PrefixIssuePath(issue, path)));

        if (supportedLanguages.Count == 0)
        {
            return;
        }

        foreach (var missingLanguage in GetMissingLanguages(selectionDialog, supportedLanguages))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorLanguageInvariantBroken,
                Message: $"selectionDialog must support application language '{missingLanguage}'.",
                Path: $"{path}.supportedLanguages",
                Id: missingLanguage));
        }
    }

    private static IReadOnlyList<string> GetMissingLanguages(
        ApplicationSelectionDialog selectionDialog,
        IReadOnlyList<string> supportedLanguages)
    {
        return supportedLanguages
            .Where(language => !selectionDialog.SupportedLanguages.Contains(language, StringComparer.Ordinal))
            .ToArray();
    }

    private static PreflightIssue PrefixIssuePath(PreflightIssue issue, string prefix)
    {
        var issuePath = issue.Path ?? "$";
        var prefixedPath = string.Equals(issuePath, "$", StringComparison.Ordinal)
            ? prefix
            : $"{prefix}{issuePath[1..]}";

        return issue with { Path = prefixedPath };
    }
}
