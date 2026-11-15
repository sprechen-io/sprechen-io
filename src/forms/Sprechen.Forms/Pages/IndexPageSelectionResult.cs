using Sprechen.Forms.Pages.Models;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Trägt Sprachliste, Optionslisten, Auswahl, Navigationsmanifest und Fortschrittszähler vom Resolver zur Index-Seite.
/// </summary>
internal sealed class IndexPageSelectionResult
{
    public IReadOnlyList<string> Languages { get; init; } = Array.Empty<string>();

    public string SelectedLanguage { get; init; } = "de";

    public IReadOnlyList<ApplicationOption> Applications { get; init; } = Array.Empty<ApplicationOption>();

    public string SelectedApplicationId { get; init; } = string.Empty;

    public ApplicationDescriptor? SelectedApplication { get; init; }

    public IReadOnlyList<FormOption> Forms { get; init; } = Array.Empty<FormOption>();

    public string SelectedFormId { get; init; } = string.Empty;

    public FormOption? SelectedForm { get; init; }

    public string NavigationManifestJson { get; init; } = "{}";

    public int CurrentFormIndex { get; init; }

    public int TotalFormsCount { get; init; }
}
