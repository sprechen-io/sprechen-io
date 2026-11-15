namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Trägt die angenommene stille Feldaktion samt Auftrags- und Oberflächenbezug weiter, damit ihre
/// Rückmeldung dem verarbeiteten Eingabestand zugeordnet bleibt.
/// </summary>
internal sealed record SpeechAssistantSilentInputAcceptedEvent(
    string CorrelationId,
    string SessionId,
    long OriginSurfaceRevision,
    long PublishedSurfaceRevision,
    string ApplicationId,
    string FormId,
    string FieldId,
    string OperationId,
    FormSilentInputAcceptedIntentKind IntentKind) : SpeechAssistantEvent;
