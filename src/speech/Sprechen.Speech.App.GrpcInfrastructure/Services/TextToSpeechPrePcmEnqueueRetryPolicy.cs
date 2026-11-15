using Grpc.Core;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Erlaubt eine TTS-Wiederholung nur nach dem ersten physischen Versuch vor
/// dem PCM-Einreihen, ohne Abbruchsignal und bei dem hier geprüften internen Netzwerkfehler.
/// </summary>
/// <remarks>
/// Erforderlich sind <c>StatusCode.Internal</c> und gemeinsam die Detailtexte
/// <c>Error reading next message</c>, <c>HttpRequestException</c> und
/// <c>TypeError: network error</c>. Abbrüche durch Aufrufer, ersetzende Wiedergabe
/// oder Nutzereinwurf schließen die Wiederholung aus.
/// </remarks>
internal sealed class TextToSpeechPrePcmEnqueueRetryPolicy
{
    internal TextToSpeechRetryEligibility Evaluate(TextToSpeechAttemptFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);

        if (failure.PhysicalAttempt != 1)
        {
            return TextToSpeechRetryEligibility.Ineligible;
        }

        if (failure.EnqueueState != TextToSpeechPcmEnqueueState.NotStarted)
        {
            return TextToSpeechRetryEligibility.Ineligible;
        }

        if (failure.CallerCancelled || failure.PlaybackSupersessionCancelled || failure.BargeInCancelled)
        {
            return TextToSpeechRetryEligibility.Ineligible;
        }

        if (failure.StatusCode != StatusCode.Internal)
        {
            return TextToSpeechRetryEligibility.Ineligible;
        }

        var detail = failure.Detail;
        if (detail.Contains("Error reading next message", StringComparison.Ordinal)
            && detail.Contains("HttpRequestException", StringComparison.Ordinal)
            && detail.Contains("TypeError: network error", StringComparison.Ordinal))
        {
            return TextToSpeechRetryEligibility.Eligible;
        }

        return TextToSpeechRetryEligibility.Ineligible;
    }
}
