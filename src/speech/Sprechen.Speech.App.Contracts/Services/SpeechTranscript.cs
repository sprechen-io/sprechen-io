namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Transportiert erkannten Text und eine optional verfügbare Konfidenz in Prozent zur weiteren Dialogverarbeitung.
/// </summary>
public sealed record SpeechTranscript(
    string Text,
    int? ConfidencePercent);
