namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Fünf kanonische Klassen fachlicher Zustandsänderungen. Ein gezählter Schritt gehört zu einer dieser Klassen. Gezählt werden keine Äußerungen.
/// </summary>
public static class TelemetryCanonicalClasses
{
    public const string FieldTargetChanged = "field_target_changed";

    public const string FieldGroupTargetChanged = "field_group_target_changed";

    public const string ConfirmedFieldValueChanged = "confirmed_field_value_changed";

    public const string FieldCompletionChanged = "field_completion_changed";

    public const string FieldGroupCompletionChanged = "field_group_completion_changed";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        FieldTargetChanged,
        FieldGroupTargetChanged,
        ConfirmedFieldValueChanged,
        FieldCompletionChanged,
        FieldGroupCompletionChanged,
    };
}
