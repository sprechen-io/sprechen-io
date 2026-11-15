using Sprechen.Common.Cqrs;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormEditing;

/// <summary>
/// Fordert das Setzen eines Feldwerts mit Zuordnung zum angegebenen Ereignisstrom an.
/// </summary>
/// <param name="SessionId">Die Sitzungs- beziehungsweise Aggregatkennung des Ereignisstroms.</param>
/// <param name="FieldId">Die typisierte Kennung des zu ändernden Felds.</param>
/// <param name="Value">Der neue Wert.</param>
internal sealed record SetFieldValueCommand(string SessionId, FieldId FieldId, string Value) : ICommand<SetFieldValueResult>;


