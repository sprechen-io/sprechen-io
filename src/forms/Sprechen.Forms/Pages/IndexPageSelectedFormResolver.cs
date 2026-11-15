using Sprechen.Forms.Pages.Models;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Wählt das Formular: angefragte ID nur bei unveränderter Anwendung, sonst Einzelformular. Ohne Vertragsversion keine Auswahl.
/// </summary>
internal sealed class IndexPageSelectedFormResolver
{
    public IndexPageSelectedFormSelection Resolve(
        IndexPageSelectionRequest request,
        string selectedApplicationId,
        IReadOnlyList<FormOption> forms)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(forms);

        var requestedFormId = ResolveRequestedFormId(request, selectedApplicationId);
        var selectedFormId = ResolveKnownFormId(requestedFormId, forms);
        var currentFormIndex = ResolveCurrentFormIndex(forms, selectedFormId);
        var selectedForm = ResolveSelectedForm(forms, selectedFormId);

        return selectedForm is null || string.IsNullOrWhiteSpace(selectedForm.ContractVersion)
            ? new IndexPageSelectedFormSelection(string.Empty, null, currentFormIndex)
            : new IndexPageSelectedFormSelection(selectedFormId, selectedForm, currentFormIndex);
    }

    private string ResolveRequestedFormId(IndexPageSelectionRequest request, string selectedApplicationId)
    {
        var previousApplicationId = Normalize(request.PreviousApplicationId);
        var selectedApplicationHasChanged = previousApplicationId.Length > 0
            && !string.Equals(previousApplicationId, selectedApplicationId, StringComparison.OrdinalIgnoreCase);

        return Normalize(selectedApplicationHasChanged ? string.Empty : request.FormId);
    }

    private string ResolveKnownFormId(string requestedFormId, IReadOnlyList<FormOption> forms)
    {
        var knownRequestedFormId = ResolveKnownRequestedFormId(requestedFormId, forms);

        return knownRequestedFormId.Length > 0 ? knownRequestedFormId : ResolveSingleFormIdOrEmpty(forms);
    }

    private string ResolveKnownRequestedFormId(string requestedFormId, IReadOnlyList<FormOption> forms)
    {
        return requestedFormId.Length > 0 && forms.Any(form => IsFormId(form, requestedFormId))
            ? requestedFormId
            : string.Empty;
    }

    private string ResolveSingleFormIdOrEmpty(IReadOnlyList<FormOption> forms)
    {
        return forms.Count == 1 ? forms[0].Id : string.Empty;
    }

    private FormOption? ResolveSelectedForm(IReadOnlyList<FormOption> forms, string selectedFormId)
    {
        return selectedFormId.Length == 0 ? null : forms.FirstOrDefault(form => IsFormId(form, selectedFormId));
    }

    private int ResolveCurrentFormIndex(IReadOnlyList<FormOption> forms, string selectedFormId)
    {
        for (var i = 0; i < forms.Count; i++)
        {
            if (IsFormId(forms[i], selectedFormId))
            {
                return i + 1;
            }
        }

        return 0;
    }

    private bool IsFormId(FormOption form, string selectedFormId)
    {
        return string.Equals(form.Id, selectedFormId, StringComparison.OrdinalIgnoreCase);
    }

    private string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
