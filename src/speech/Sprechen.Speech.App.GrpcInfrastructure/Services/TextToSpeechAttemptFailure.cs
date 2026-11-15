using Grpc.Core;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Hält Versuchszähler, Transportfehler, PCM-Einreihestatus und die drei
/// Abbruchsignale fest, auf deren Grundlage eine TTS-Wiederholung geprüft wird.
/// </summary>
internal sealed class TextToSpeechAttemptFailure
{
    internal TextToSpeechAttemptFailure(
        int physicalAttempt,
        StatusCode statusCode,
        string detail,
        TextToSpeechPcmEnqueueState enqueueState,
        bool callerCancelled,
        bool playbackSupersessionCancelled,
        bool bargeInCancelled)
    {
        PhysicalAttempt = physicalAttempt;
        StatusCode = statusCode;
        Detail = detail ?? string.Empty;
        EnqueueState = enqueueState;
        CallerCancelled = callerCancelled;
        PlaybackSupersessionCancelled = playbackSupersessionCancelled;
        BargeInCancelled = bargeInCancelled;
    }

    internal int PhysicalAttempt { get; }
    internal StatusCode StatusCode { get; }
    internal string Detail { get; }
    internal TextToSpeechPcmEnqueueState EnqueueState { get; }
    internal bool CallerCancelled { get; }
    internal bool PlaybackSupersessionCancelled { get; }
    internal bool BargeInCancelled { get; }
}
