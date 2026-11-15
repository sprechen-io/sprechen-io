namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält fest, ob ein Navigationsbefehl behandelt wurde, und enthält gegebenenfalls das Dialogergebnis.
/// </summary>
internal sealed record FormVoiceCommandSwitchFormNavigationResult(
    bool WasHandled,
    VoiceCommandDialogResult? DialogResult)
{
    public static FormVoiceCommandSwitchFormNavigationResult NotHandled { get; } = new(false, null);

    public static FormVoiceCommandSwitchFormNavigationResult Handled(VoiceCommandDialogResult? dialogResult)
        => new(true, dialogResult);
}
