namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Identifiziert Sitzung, Formular, Feld und Ansichtsrevision zur Prüfung eingehender stiller Eingabeaktionen.
/// </summary>
public sealed record SpeechAssistantSilentInputSnapshotIdentity(
    string CorrelationId,
    string SessionId,
    long SurfaceRevision,
    string ApplicationId,
    string FormId,
    string FieldId);
