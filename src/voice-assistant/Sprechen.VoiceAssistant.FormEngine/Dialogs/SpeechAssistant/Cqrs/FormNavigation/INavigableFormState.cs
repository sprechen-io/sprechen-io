using Sprechen.Common.Cqrs;
using Sprechen.Mda.Contracts.V1.Identifiers;
namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;

/// <summary>
/// Stellt Fokus, Feldreihenfolge und Eingabeverfügbarkeit für die Suche und den Wechsel
/// zwischen benachbarten Formularfeldern bereit.
/// </summary>
public interface INavigableFormState
{
    /// <summary>
    /// Liest die Fokuskennung oder null, wenn kein Feld fokussiert ist.
    /// </summary>
    FieldId? CurrentFocusedFieldId { get; }

    string[] OrderedFieldIds { get; }

    /// <summary>
    /// Prüft die aktuelle Eingabeverfügbarkeit unter Berücksichtigung von Sichtbarkeit,
    /// Aktivierung, Laufzeitschreibschutz und Bearbeitbarkeit.
    /// </summary>
    bool IsFieldInputAvailable(FieldId fieldId);

    void SetCurrentFocus(FieldId fieldId);

    bool TryGetFieldIndex(FieldId fieldId, out int index);
}
