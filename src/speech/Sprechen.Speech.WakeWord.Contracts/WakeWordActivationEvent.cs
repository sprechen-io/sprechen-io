namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Beschreibt eine Aktivierung über Statusdaten und Textlängen, damit ihre Diagnose ohne Übertragung des Wortlauts auskommt.
/// </summary>
public sealed record WakeWordActivationEvent(
    string CorrelationId,
    WakeWordActivationPhase Phase,
    bool CommandTextPresent,
    int TranscriptLength,
    int CommandTextLength,
    int? ConfidencePercent);
