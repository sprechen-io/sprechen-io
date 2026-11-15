namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Optionaler Studienkontext der Sitzung mit Teilnehmerkennung.
/// Die Telemetrie verwendet ihn zur Zuordnung. Die Aufgabe ergibt sich aus dem laufenden Formular.
/// </summary>
public interface IStudyTelemetryContext
{
    string? ParticipantId { get; }

    void SetStudyContext(string? participantId);
}
