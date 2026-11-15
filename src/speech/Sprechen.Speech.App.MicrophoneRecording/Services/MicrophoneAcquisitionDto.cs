namespace Sprechen.Speech.App.MicrophoneRecording.Services;

/// <summary>
/// Überträgt Sitzungskennung oder Browserfehler der Mikrofonanforderung,
/// damit die Mikrofonverwaltung das Ergebnis vor der Verwendung prüfen kann.
/// </summary>
internal sealed class MicrophoneAcquisitionDto
{
    public string? SessionId { get; set; }

    public string? ErrorName { get; set; }
}
