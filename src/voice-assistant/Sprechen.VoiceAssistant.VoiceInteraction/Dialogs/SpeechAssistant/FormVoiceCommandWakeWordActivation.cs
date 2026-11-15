using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft ein führendes Weckwort anhand des Dialoginventars und liefert bei Aktivierung den nicht leeren, bereinigten Befehlstext für den Formularlauf.
/// </summary>
internal static class FormVoiceCommandWakeWordActivation
{
    public static bool TryExtractActivatedCommandTranscript(
        FormVoiceCommandContext context,
        string transcript,
        out string activatedCommandTranscript)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);

        activatedCommandTranscript = string.Empty;

        if (context.WakeWordActivationForms.IsDefaultOrEmpty
            && context.WakeWordRecognitionOnlyActivationForms.IsDefaultOrEmpty)
        {
            return false;
        }

        var wakeWordInventory = new WakeWordInventory(
            context.WakeWordActivationForms,
            context.WakeWordRecognitionOnlyActivationForms);
        var evaluation = WakeWordActivationRule.EvaluateLeadingWakeWord(
            transcript,
            confidencePercent: null,
            wakeWordInventory);

        if (!evaluation.IsActivated || string.IsNullOrWhiteSpace(evaluation.CommandText))
        {
            return false;
        }

        activatedCommandTranscript = evaluation.CommandText.Trim();
        return true;
    }
}
