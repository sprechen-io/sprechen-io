using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Navigation der Formularlaufzeit: Fokus vor und zurück, Sprung zu einem Ziel,
/// Feldgruppenwechsel und Suche nach dem nächsten offenen Pflichtfeld. Sprünge und
/// Pflichtfeldsuche liefern Ergebnisse, die die Dialogsteuerung gezielt auswerten kann.
/// </summary>
public interface IFormDialogRuntimeNavigation
{
    Task<bool> TryMoveFocusNextAsync();

    Task<bool> TryMoveFocusBackAsync();

    /// <summary>
    /// Löst ein gesprochenes Sprungziel auf. Ein bearbeitbares Ziel erhält den Fokus
    /// (FormJumpToResultKind.Focused). Ein schreibgeschütztes Ziel liefert nach den Datenschutzregeln
    /// aufbereitete Vorlesedaten (SpokenReadOnlyTarget). Ein unbekanntes Ziel ergibt NotFound.
    /// </summary>
    Task<FormJumpToResult> TryJumpToAsync(string target);

    Task<bool> TryNavigateToFieldGroupAsync(string fieldGroupId);

    Task<FocusNextOpenRequiredFieldResult> TryFocusNextOpenRequiredFieldAsync();
}
