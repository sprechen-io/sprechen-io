using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Ermittelt, welche Erkennungsergebnisse oder Ersatzgründe eine Aktivierungsanfrage
/// enthält. Die anschließende Konfliktprüfung entscheidet über ihre Zulässigkeit.
/// </summary>
internal sealed class WakeArbitrationInputShapeReader
{
    public WakeArbitrationInputShape Read(WakeArbitrationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new WakeArbitrationInputShape(
            input.FallbackReason != LexicalWakeFallbackReason.None,
            input.AcousticEvidence is not null,
            input.LexicalEvaluation is not null,
            input.StitchResult is not null);
    }
}
