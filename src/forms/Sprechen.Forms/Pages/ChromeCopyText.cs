/// <summary>
/// Enthält einen Oberflächentext in fünf Sprachen aus dem gemeinsamen Textkatalog.
/// Die Positionsargumente sind in der Reihenfolge de, en, fr, es, it angeordnet.
/// </summary>
internal sealed record ChromeCopyText(string De, string En, string Fr, string Es, string It)
{
    /// <summary>
    /// Wählt den Text zur Sprachkennung. Für andere oder fehlende Kennungen wird Deutsch verwendet.
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
