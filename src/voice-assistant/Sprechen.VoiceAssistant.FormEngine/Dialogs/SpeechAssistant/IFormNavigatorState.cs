using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Zugriff auf Fokus, Feldfolge und Verfügbarkeitsprüfungen für die Formularnavigation.
/// </summary>
public interface IFormNavigatorState
{
    FieldId? CurrentFocusedFieldId { get; }

    ImmutableArray<string> OrderedFieldIds { get; }

    void SetCurrentFocus(FieldId fieldId);

    bool TryGetFieldIndex(FieldId fieldId, out int index);

    /// <summary>
    /// Prüft die aktuelle Eingabeverfügbarkeit aus Sichtbarkeit, Aktivierung, Schreibschutz und Bearbeitbarkeit.
    /// </summary>
    bool IsFieldInputAvailable(FieldId fieldId);

    /// <summary>
    /// Prüft die Verfügbarkeit für Ansagen: eingabefähige Felder oder sichtbare schreibgeschützte Anzeigefelder.
    /// </summary>
    bool IsFieldAudiblyAvailable(FieldId fieldId);

    bool IsKnownField(FieldId fieldId);

    bool IsKnownFieldGroup(FieldGroupId fieldGroupId);

    /// <summary>
    /// Ermittelt die Gruppe, die das Feld unmittelbar enthält.
    /// </summary>
    FieldGroupId? GetFieldGroupId(FieldId fieldId);

    /// <summary>
    /// Prüft, ob die Zielgruppe ein Vorfahr der angegebenen Gruppe oder die Gruppe selbst ist.
    /// </summary>
    bool IsFieldGroupOnPath(FieldGroupId fieldGroupId, FieldGroupId targetFieldGroupId);
}
