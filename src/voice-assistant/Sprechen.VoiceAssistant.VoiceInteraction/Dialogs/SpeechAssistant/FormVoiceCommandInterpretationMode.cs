namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt Kennzeichen für die Auswertung eines Transkripts während
/// der aktuellen Feldeingabe.
/// </summary>
public sealed record FormVoiceCommandInterpretationMode(
    bool IsTextField,
    bool IsLiteralMode,
    bool IsDigitsMode,
    bool ShouldExecuteParsedCommand,
    bool ShouldExecuteParsedCommandInDigitsMode,
    bool IsSpellingMode = false);
