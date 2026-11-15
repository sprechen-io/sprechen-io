/// <summary>
/// Bündelt die übersetzten Beschriftungen für helles, dunkles und
/// systemabhängiges Farbschema zur gemeinsamen Auswahl.
/// </summary>
internal static class VoiceThemeOptionCopyData
{
    internal static readonly IReadOnlyDictionary<string, ChromeCopyText> Entries =
        new Dictionary<string, ChromeCopyText>(StringComparer.Ordinal)
        {
        ["GetOptionLabel.Dark"] = new ChromeCopyText("Dunkel", "Dark", "Sombre", "Oscuro", "Scuro"),
        ["GetOptionLabel.Light"] = new ChromeCopyText("Hell", "Light", "Clair", "Claro", "Chiaro"),
        ["GetOptionLabel.System"] = new ChromeCopyText("System", "System", "Système", "Sistema", "Sistema"),
        };

    internal static string Select(string copyId, string? language)
        => Entries[copyId].For(language);
}
