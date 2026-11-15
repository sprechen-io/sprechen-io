using Sprechen.Forms.Pages.Models;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Enthält Formular-ID, optionales Formular und die für die Fortschrittsanzeige
/// ermittelte Position der Formularauswahl.
/// </summary>
internal sealed class IndexPageSelectedFormSelection
{
    public IndexPageSelectedFormSelection(string selectedFormId, FormOption? selectedForm, int currentFormIndex)
    {
        SelectedFormId = selectedFormId;
        SelectedForm = selectedForm;
        CurrentFormIndex = currentFormIndex;
    }

    public string SelectedFormId { get; }

    public FormOption? SelectedForm { get; }

    public int CurrentFormIndex { get; }
}
