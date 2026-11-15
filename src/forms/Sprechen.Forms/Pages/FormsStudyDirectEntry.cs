using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Ermittelt die markierte Studienanwendung und ihr erstes Formular,
/// damit ein Studienlink direkt zur Bearbeitung führt.
/// </summary>
internal static class FormsStudyDirectEntry
{
    /// <summary>
    /// Liefert die IDs der einzigen Studienanwendung und ihres ersten Formulars
    /// in der durch Sequence und Formular-ID festgelegten Reihenfolge.
    /// Fehlt eine eindeutige Anwendung oder ein Formular, wird eine Ausnahme ausgelöst.
    /// </summary>
    public static (string ApplicationId, string FormId) Bind(FormsCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var studyApplications = catalog.Applications
            .Where(application => application.IsStudyApplication == true)
            .ToArray();
        if (studyApplications.Length != 1)
        {
            throw new InvalidOperationException(
                "A study link requires exactly one application marked as the study application.");
        }

        var application = studyApplications[0];
        if (application.Forms.Count == 0)
        {
            throw new InvalidOperationException(
                "The study application does not declare a form.");
        }

        var form = application.Forms
            .OrderBy(item => item.Sequence)
            .ThenBy(item => item.FormId.ToString(), StringComparer.Ordinal)
            .First();
        return (application.ApplicationId, form.FormId.ToString()!);
    }
}
