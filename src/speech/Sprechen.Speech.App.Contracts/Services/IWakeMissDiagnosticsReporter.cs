namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>Übermittelt Diagnosedaten zu Fehlschlägen und Wiederherstellung während eines Aktivierungsversuchs.</summary>
/// <remarks>
/// Ergänzt den Erfassungsbericht um die Auswertung des Aktivierungsworts, die auf dem Client erfolgt.
/// Fehler der Diagnoseübermittlung dürfen die Zuhörschleife nicht unterbrechen.
/// Die Übermittlung darf den nächsten Aktivierungsversuch nicht aufhalten.
/// </remarks>
public interface IWakeMissDiagnosticsReporter
{
    ValueTask ReportAsync(WakeMissDiagnosticsReport report, CancellationToken cancellationToken);
}
