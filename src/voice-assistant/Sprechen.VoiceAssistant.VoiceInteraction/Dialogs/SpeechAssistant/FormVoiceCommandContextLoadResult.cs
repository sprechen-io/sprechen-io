namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält das Ladeergebnis des Befehlskontexts oder ein Abbruchergebnis bereit.
/// Die Fabrikmethoden erzeugen jeweils eine dieser beiden Varianten.
/// </summary>
public sealed record FormVoiceCommandContextLoadResult(
    FormVoiceCommandContext? Context,
    VoiceCommandDialogResult? AbandonedResult)
{
    public static FormVoiceCommandContextLoadResult FromContext(FormVoiceCommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new FormVoiceCommandContextLoadResult(context, AbandonedResult: null);
    }

    public static FormVoiceCommandContextLoadResult FromAbandoned(VoiceCommandDialogResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new FormVoiceCommandContextLoadResult(Context: null, result);
    }
}

