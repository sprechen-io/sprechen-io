namespace Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Hält einen bewerteten Befehlsfund samt Argument und Phrasenlänge für die Trefferauswahl.
/// </summary>
public readonly record struct GlobalCommandCandidate(
    string CommandId,
    string? Argument,
    double Score,
    int MatchedUtteranceTokenCount);

