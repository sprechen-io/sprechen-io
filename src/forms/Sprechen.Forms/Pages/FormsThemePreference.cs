namespace Sprechen.Forms.Pages;

/// <summary>
/// Normalisiert die Auswahl des Farbschemas für Cookie und Seitenausgabe.
/// Bei System wird kein data-theme-Attribut gesetzt. Bei Light oder Dark wird die Auswahl
/// als Attribut bereits im serverseitig erzeugten HTML ausgegeben.
/// </summary>
internal sealed class FormsThemePreference
{
    public const string System = "system";
    public const string Light = "light";
    public const string Dark = "dark";

    /// <summary>
    /// Die wählbaren Werte in Anzeigereihenfolge für die Einstellungsseite.
    /// </summary>
    public static readonly IReadOnlyList<string> SupportedValues = new[] { System, Light, Dark };

    private readonly string _value;

    private FormsThemePreference(string value) => _value = value;

    /// <summary>
    /// Normalisiert eine Farbschema-Angabe unabhängig von Groß- und Kleinschreibung
    /// und äußeren Leerzeichen. Fehlende oder unbekannte Werte ergeben System.
    /// </summary>
    public static FormsThemePreference From(string? value) => new(Normalize(value));

    /// <summary>
    /// Kanonischer Wert zum Speichern im Cookie und als gewählte Option im Select.
    /// </summary>
    public string Value => _value;

    /// <summary>
    /// Wert des `data-theme`-Attributs am `html`-Element, oder null für `System`
    /// (Razor lässt das Attribut dann weg, damit die Media-Query gilt).
    /// </summary>
    public string? DataThemeAttribute => _value switch
    {
        Light => Light,
        Dark => Dark,
        _ => null,
    };

    /// <summary>
    /// Prüft die exakte Übereinstimmung mit dem gespeicherten Wert
    /// für die Markierung der gewählten Option.
    /// </summary>
    public bool Is(string candidate) => string.Equals(_value, candidate, StringComparison.Ordinal);

    /// <summary>
    /// Prüft, ob eine Angabe nach Entfernen äußerer Leerzeichen
    /// und unabhängig von Groß- und Kleinschreibung unterstützt wird.
    /// </summary>
    public static bool IsSupported(string? value)
        => value is not null
           && SupportedValues.Contains((value).Trim().ToLowerInvariant());

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        Light => Light,
        Dark => Dark,
        System => System,
        _ => System,
    };
}
