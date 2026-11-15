namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Bezugsgrößen für Fortschrittsangaben mit einem Nenner von null.
/// </summary>
public static class TelemetryZeroDenominatorModes
{
    /// <summary>
    /// Fortschritt bezogen auf alle Felder.
    /// </summary>
    public const string AllFields = "allFields";
    /// <summary>
    /// Fortschritt bezogen nur auf Pflichtfelder.
    /// </summary>
    public const string RequiredFieldsOnly = "requiredFieldsOnly";
    /// <summary>
    /// Fortschritt bezogen auf Feldgruppen.
    /// </summary>
    public const string FieldGroupBased = "fieldGroupBased";

    /// <summary>
    /// Alle zulässigen Bezugsgrößen für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        AllFields,
        RequiredFieldsOnly,
        FieldGroupBased,
    };
}
