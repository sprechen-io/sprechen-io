namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Unterscheidet fehlendes, möglicherweise begonnenes und bestätigtes
/// Einreihen von PCM-Daten als Grundlage der TTS-Wiederholungsentscheidung.
/// </summary>
internal enum TextToSpeechPcmEnqueueState
{
    NotStarted,
    PossiblyEnqueued,
    Enqueued,
}
