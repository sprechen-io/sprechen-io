using Sprechen.Forms.Pages.Models;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Bündelt die aufgelöste Auswahl — Anwendung, Formular, Anwendungs-ID, Sprache — als Eingabe für das Vertragsladen.
/// </summary>
internal sealed class IndexPageSelectedFormRequest
{
    public IndexPageSelectedFormRequest(
        ApplicationDescriptor selectedApplication,
        FormOption selectedForm,
        string selectedApplicationId,
        string selectedLanguage)
    {
        SelectedApplication = selectedApplication ?? throw new ArgumentNullException(nameof(selectedApplication));
        SelectedForm = selectedForm ?? throw new ArgumentNullException(nameof(selectedForm));
        SelectedApplicationId = selectedApplicationId;
        SelectedLanguage = selectedLanguage;
    }

    public ApplicationDescriptor SelectedApplication { get; }

    public FormOption SelectedForm { get; }

    public string SelectedApplicationId { get; }

    public string SelectedLanguage { get; }
}
