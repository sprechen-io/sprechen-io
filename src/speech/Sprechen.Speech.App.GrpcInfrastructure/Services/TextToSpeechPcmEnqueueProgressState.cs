namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Verfolgt, ob PCM-Daten noch nicht, möglicherweise bereits oder
/// nachweislich erfolgreich eingereiht wurden.
/// </summary>
internal sealed class TextToSpeechPcmEnqueueProgressState
{
    internal TextToSpeechPcmEnqueueState State { get; private set; } = TextToSpeechPcmEnqueueState.NotStarted;

    internal void MarkEnqueueStarted()
    {
        if (State == TextToSpeechPcmEnqueueState.NotStarted)
        {
            State = TextToSpeechPcmEnqueueState.PossiblyEnqueued;
        }
    }

    internal void MarkEnqueued()
    {
        if (State == TextToSpeechPcmEnqueueState.PossiblyEnqueued)
        {
            State = TextToSpeechPcmEnqueueState.Enqueued;
        }
    }
}
