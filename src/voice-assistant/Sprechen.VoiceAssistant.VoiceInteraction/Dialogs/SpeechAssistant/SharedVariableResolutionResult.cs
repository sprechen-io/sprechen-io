using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält aufgelöste gemeinsame Werte und ihre Herkunft zusammen, damit Übernahme und Ansage denselben Auflösungsstand verwenden.
/// </summary>
internal sealed record SharedVariableResolutionResult(
    ImmutableDictionary<string, string> ValuesByVariableId,
    ImmutableArray<(string VariableId, string? SourceFormId)> Adoptions);
