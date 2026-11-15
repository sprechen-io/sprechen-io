namespace Sprechen.ServiceDefaults;

/// <summary>
/// Bündelt die Namen der Meter und ActivitySources, die der Host abonnieren soll,
/// damit deren Messdaten in die gemeinsame Telemetrie-Konfiguration einfließen.
/// </summary>
internal static class SprechenOpenTelemetrySources
{
    public const string SpeechRecognitionMeterName = "Sprechen.Service.SpeechRecognition";

    public static string[] GetMeterNames()
    {
        return
        [
            SpeechRecognitionMeterName
        ];
    }

    public static string[] GetActivitySourceNames(string applicationName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);

        return
        [
            applicationName
        ];
    }
}
