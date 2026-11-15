using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Wählt aus der zuvor geprüften Eingabeform die akustische oder lexikalische Entscheidungsroute
/// beziehungsweise den angeforderten Ersatzweg.
/// </summary>
internal sealed class WakeArbitrationRouteClassifier
{
    public WakeArbitrationRoute Classify(WakeArbitrationInput input, WakeArbitrationInputShape shape)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (shape.HasFallback)
        {
            return WakeArbitrationRoute.LexicalFallback;
        }

        if (shape.HasAcoustic)
        {
            return WakeArbitrationRoute.AcousticActivation;
        }

        return input.LexicalEvaluation!.IsActivated
            ? WakeArbitrationRoute.LexicalActivation
            : WakeArbitrationRoute.LexicalMissOrStitch;
    }
}
