namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Geltungsbereich eines kanonischen Übergangs: ein Feld oder eine Feldgruppe.
/// </summary>
public static class TelemetryTransitionScopes
{
    public const string Field = "field";

    public const string FieldGroup = "field_group";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Field,
        FieldGroup,
    };
}
