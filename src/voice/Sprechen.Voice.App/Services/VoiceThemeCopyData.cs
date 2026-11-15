/// <summary>
/// Bündelt die übersetzten Erläuterungen zur Auswahl des Farbschemas.
/// </summary>
internal static class VoiceThemeCopyData
{
    internal static readonly IReadOnlyDictionary<string, ChromeCopyText> Entries =
        new Dictionary<string, ChromeCopyText>(StringComparer.Ordinal)
        {
        ["GetDescription"] = new ChromeCopyText("Wählen Sie das Farbschema der Anwendung.", "Choose the application's color scheme.", "Choisissez le thème de couleur de l'application.", "Elija el esquema de color de la aplicación.", "Scegli la combinazione di colori dell'applicazione."),
        ["GetSystemHint"] = new ChromeCopyText("Folgt der Einstellung Ihres Geräts.", "Follows your device setting.", "Suit le réglage de votre appareil.", "Sigue la configuración de su dispositivo.", "Segue l'impostazione del tuo dispositivo."),
        ["GetTitle"] = new ChromeCopyText("Erscheinungsbild", "Appearance", "Apparence", "Apariencia", "Aspetto"),
        };

    internal static string Select(string copyId, string? language)
        => Entries[copyId].For(language);
}
