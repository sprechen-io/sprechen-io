namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Auslöser einer aufgezeichneten Validierungsfehlermeldung. Der Zustand allein
/// zeigt nicht, welche Handlung zur Meldung geführt hat.
/// </summary>
public static class TelemetryErrorTriggers
{
    /// <summary>
    /// Validierungsfehlermeldung beim Laden, vor einer Eingabe.
    /// </summary>
    public const string Load = "load";

    /// <summary>
    /// Validierungsfehlermeldung durch eine Eingabe oder Änderung der Person.
    /// </summary>
    public const string UserInput = "user_input";

    /// <summary>
    /// Validierungsfehlermeldung durch einen Abgabeversuch.
    /// </summary>
    public const string SubmitAttempt = "submit_attempt";

    /// <summary>
    /// Eine bereits verletzte Regel wird sichtbar, ohne dass ein neuer Versuch stattfindet.
    /// </summary>
    public const string RuleVisibility = "rule_visibility";

    /// <summary>
    /// Fehlermeldung aus der Erkennung des Systems statt aus der Prüfung eines Feldwerts.
    /// </summary>
    public const string SystemRecognition = "system_recognition";

    /// <summary>
    /// Alle zulässigen Auslöser für die Prüfung aufgezeichneter Werte.
    /// </summary>
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Load,
        UserInput,
        SubmitAttempt,
        RuleVisibility,
        SystemRecognition,
    };
}
