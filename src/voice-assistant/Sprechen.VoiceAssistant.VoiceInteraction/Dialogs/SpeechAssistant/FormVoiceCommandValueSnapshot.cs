using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält Formularwerte und Übernahmen gemeinsamer Variablen für einen Sitzungslauf zusammen.
/// </summary>
public sealed record FormVoiceCommandValueSnapshot(
    ImmutableDictionary<string, string> ValuesByVariableId,
    ImmutableArray<(string VariableId, string? SourceFormId)> SharedAdoptions);
