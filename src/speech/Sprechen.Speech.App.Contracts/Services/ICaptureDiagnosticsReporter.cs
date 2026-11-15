namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>Übermittelt Diagnosedaten eines Erfassungsversuchs.</summary>
/// <remarks>
/// Ein Versuch ohne erkanntes Sprachsegment erreicht den Erkennungsdienst nicht.
/// Der Bericht macht auch solche Fälle außerhalb des Geräts unterscheidbar, etwa
/// einen fehlgeschlagenen Mikrofonstart und eine Aufnahme ohne Sprachergebnis.
/// Fehler der Diagnoseübermittlung dürfen die Erfassung nicht unterbrechen.
/// </remarks>
public interface ICaptureDiagnosticsReporter
{
    ValueTask ReportAsync(CaptureDiagnosticsReport report, CancellationToken cancellationToken);
}
