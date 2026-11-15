namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Formatiert optionale Konfidenzwerte für die Aktivierungsdiagnose einheitlich,
/// damit fehlende Werte von tatsächlich gemessenen Zahlen unterscheidbar bleiben.
/// </summary>
internal sealed class WakeWordTelemetryValueFormatter
{
    public string FormatConfidence(int? confidencePercent)
    {
        return confidencePercent.HasValue
            ? confidencePercent.Value.ToString()
            : "<null>";
    }
}
