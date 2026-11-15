namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Zustände nach kanonischen Übergängen, unabhängig vom Dialogzustand.
/// Das gemeinsame Vokabular hält beide Studienbedingungen vergleichbar.
/// </summary>
public static class TelemetryResultingStates
{
    public const string Targeted = "targeted";

    public const string ConfirmedChanged = "confirmed_changed";

    public const string Complete = "complete";

    public const string Incomplete = "incomplete";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Targeted,
        ConfirmedChanged,
        Complete,
        Incomplete,
    };
}
