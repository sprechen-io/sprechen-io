namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Festgelegte Geltungsbereiche von Telemetrieereignissen.
/// </summary>
public static class TelemetryScopes
{
    /// <summary>
    /// Geltungsbereich eines einzelnen Feldes.
    /// </summary>
    public const string Field = "field";
    /// <summary>
    /// Geltungsbereich einer Feldgruppe.
    /// </summary>
    public const string FieldGroup = "field_group";
    /// <summary>
    /// Geltungsbereich über mehrere Feldgruppen.
    /// </summary>
    public const string CrossGroup = "cross_group";
    /// <summary>
    /// Geltungsbereich des gesamten Formulars.
    /// </summary>
    public const string Form = "form";

    /// <summary>
    /// Alle zulässigen Geltungsbereiche für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Field,
        FieldGroup,
        CrossGroup,
        Form,
    };
}
