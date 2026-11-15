namespace Sprechen.Mda.Contracts.V1.Catalog;

/// <summary>
/// Bestimmt die kanonische Formularreihenfolge im Anwendungskatalog.
/// </summary>
public sealed class FormsCatalogFormOrder
{
    /// <summary>
    /// Sortiert Formulare deterministisch nach Sequence und stabiler FormId.
    /// </summary>
    public FormDescriptor[] OrderForms(ApplicationDescriptor application)
    {
        ArgumentNullException.ThrowIfNull(application);

        return application.Forms
            .Where(form => form is not null && form.FormId != default)
            .OrderBy(form => form.Sequence is null)
            .ThenBy(form => form.Sequence ?? int.MaxValue)
            .ThenBy(form => form.FormId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Liefert die erste nicht leere Formularkennung nach der Katalogsortierung oder null.
    /// </summary>
    public string? GetDefaultFormId(ApplicationDescriptor application)
    {
        ArgumentNullException.ThrowIfNull(application);

        return OrderForms(application)
            .Select(form => form.FormId.Value.Trim())
            .FirstOrDefault(formId => !string.IsNullOrWhiteSpace(formId));
    }
}
