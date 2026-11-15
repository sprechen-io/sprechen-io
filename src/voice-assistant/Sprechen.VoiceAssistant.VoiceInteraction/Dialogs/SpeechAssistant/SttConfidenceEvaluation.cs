namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält Vertrauenseinstufung und tatsächlich angewandte Bestätigungsgrenze zusammen,
/// damit eine spätere Erläuterung die Grenze nicht erneut aus dem Vertrag ableiten muss.
/// </summary>
internal readonly record struct SttConfidenceEvaluation(
    SttConfidenceLevel Level,
    int AppliedConfirmThresholdPercent);
