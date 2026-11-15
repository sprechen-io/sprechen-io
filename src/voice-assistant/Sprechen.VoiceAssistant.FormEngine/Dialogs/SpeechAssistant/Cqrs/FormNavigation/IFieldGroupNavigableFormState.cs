using Sprechen.Common.Cqrs;
using Sprechen.Mda.Contracts.V1.Identifiers;
namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;

/// <summary>
/// Stellt Gruppenbezug, Feldreihenfolge und Verfügbarkeitsprüfung bereit, damit die
/// Gruppennavigation ihr Eingabeziel im Formularzustand bestimmen und setzen kann.
/// </summary>
public interface IFieldGroupNavigableFormState
{
    FieldId? CurrentFocusedFieldId { get; }

    /// <summary>
    /// Ermittelt die aktuelle Feldgruppe aus dem Fokusbezug.
    /// </summary>
    FieldGroupId? GetCurrentFieldGroupId();

    bool IsFieldGroupKnown(FieldGroupId fieldGroupId);

    bool IsFieldGroupVisible(FieldGroupId fieldGroupId);

    /// <summary>
    /// Liefert die geordnete Feldfolge der Gruppe.
    /// </summary>
    IEnumerable<string> GetFieldsInFieldGroup(FieldGroupId fieldGroupId);

    /// <summary>
    /// Prüft die aktuelle Eingabeverfügbarkeit unter Berücksichtigung von Sichtbarkeit,
    /// Aktivierung, Laufzeitschreibschutz und Bearbeitbarkeit.
    /// </summary>
    bool IsFieldInputAvailable(FieldId fieldId);

    void SetCurrentFocus(FieldId fieldId);
}
