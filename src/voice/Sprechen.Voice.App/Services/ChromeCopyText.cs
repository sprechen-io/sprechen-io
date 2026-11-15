/// <summary>
/// Bündelt fünf Übersetzungen eines Oberflächentextes für eine gemeinsame
/// Sprachauswahl mit Deutsch als Standardsprache.
/// </summary>
internal sealed record ChromeCopyText(string De, string En, string Fr, string Es, string It)
{
    /// <summary>
    /// Wählt die angegebene Übersetzung und verwendet bei fehlendem oder unbekanntem
    /// Sprachcode den deutschen Text.
    /// </summary>
    internal string For(string? language)
        => language switch
        {
            "en" => En,
            "fr" => Fr,
            "es" => Es,
            "it" => It,
            _ => De,
        };
}
