namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält die Telemetriedimensionen eines mehrdeutigen Eingabe- oder Befehlssignals
/// für eine spätere Ausgabe zusammen.
/// </summary>
public readonly record struct AmbiguityTelemetryData(
    string AmbiguityType,
    string ResolutionStrategy,
    string CommandSignalClass,
    string Result,
    string? CommandId);

