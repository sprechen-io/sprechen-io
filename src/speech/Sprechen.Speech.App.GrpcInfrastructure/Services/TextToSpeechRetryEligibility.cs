namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Kennzeichnet, ob der geprüfte TTS-Versuchsfehler eine Wiederholung
/// vor dem ersten PCM-Einreihen zulässt.
/// </summary>
internal enum TextToSpeechRetryEligibility
{
    Eligible,
    Ineligible,
}
