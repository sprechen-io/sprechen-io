using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt Feldwerte, Fokus und detaillierten Status für die Projektion des Dialogs auf eine Formularoberfläche bereit.
/// </summary>
public interface IFormDialogSurfaceState
{
    Task<ImmutableDictionary<string, FormSurfaceFieldValue>> GetSurfaceFieldValuesAsync();

    Task<string?> GetFocusedFieldIdAsync();

    Task<FormDetailedStatusSnapshot> GetDetailedStatusSnapshotAsync();
}
