namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet die Fortsetzungsentscheidung eines Feldablaufs mit einer
/// bereits erfolgten Abgabe oder einem vorhandenen Dialogergebnis.
/// </summary>
public sealed record FormVoiceCommandFieldHandlingResult(
    bool ShouldContinue,
    bool WasSubmitted,
    VoiceCommandDialogResult? DialogResult)
{
    public static FormVoiceCommandFieldHandlingResult Continue()
        => new(true, false, null);

    public static FormVoiceCommandFieldHandlingResult Submitted()
        => new(false, true, null);

    public static FormVoiceCommandFieldHandlingResult FromDialogResult(VoiceCommandDialogResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new FormVoiceCommandFieldHandlingResult(false, false, result);
    }
}

