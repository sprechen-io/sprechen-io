using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt die Ermittlung zusätzlicher Feldwertvorschläge aus abgegrenzten
/// Namens-Wert-Segmenten bereit. Das aktuell fokussierte Feld bleibt dabei ausgeschlossen.
/// </summary>
internal interface IMultiSlotValueExtractor
{
    ImmutableArray<FormAdditionalFieldValueCandidate> Extract(string transcript, FieldId? focusedFieldId);
}
