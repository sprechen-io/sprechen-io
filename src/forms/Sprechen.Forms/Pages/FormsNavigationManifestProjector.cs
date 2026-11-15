using Sprechen.Forms.Pages.Models;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Projiziert gewählte Anwendung und Formularoptionen auf das Navigationsmanifest der clientseitigen Formularnavigation.
/// </summary>
internal sealed class FormsNavigationManifestProjector
{
    public FormsNavigationManifest Project(ApplicationDescriptor selectedApplication, IReadOnlyList<FormOption> forms)
    {
        ArgumentNullException.ThrowIfNull(selectedApplication);
        ArgumentNullException.ThrowIfNull(forms);

        var formDescriptorsById = BuildFormDescriptorsById(selectedApplication);

        return new FormsNavigationManifest
        {
            ApplicationId = selectedApplication.ApplicationId,
            Forms = forms.Select(form => BuildFormEntry(form, formDescriptorsById)).ToArray()
        };
    }

    private Dictionary<string, FormDescriptor> BuildFormDescriptorsById(ApplicationDescriptor selectedApplication)
    {
        return selectedApplication.Forms
            .Where(form => form is not null && form.FormId != default)
            .GroupBy(form => form.FormId.Value.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    private FormsNavigationManifestForm BuildFormEntry(
        FormOption form,
        IReadOnlyDictionary<string, FormDescriptor> formDescriptorsById)
    {
        formDescriptorsById.TryGetValue(form.Id, out var descriptor);

        return new FormsNavigationManifestForm
        {
            FormId = form.Id,
            DisplayName = form.DisplayName,
            PrerequisiteForms = NormalizeIds(descriptor?.PrerequisiteForms),
            SharedVariables = NormalizeIds(descriptor?.SharedVariables)
        };
    }

    private IReadOnlyList<string> NormalizeIds(IEnumerable<string>? ids)
    {
        return ids
            ?.Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .ToArray() ?? Array.Empty<string>();
    }
}
