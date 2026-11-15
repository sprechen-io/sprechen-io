namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Aufgabenweite Abweichungscodes des Abschlussereignisses. Die Laufzeit sendet
/// niemals aborted oder unclear. Die Beobachtungsakte darf sie tragen.
/// </summary>
public static class TelemetryMismatchCodes
{
    public const string None = "none";
    public const string TaskCardMismatch = "task_card_mismatch";
    public const string MissingRequired = "missing_required";
    public const string WrongFieldClass = "wrong_field_class";
    public const string WrongDependencyState = "wrong_dependency_state";
    public const string ValidationErrorOpen = "validation_error_open";
    public const string NotSubmitted = "not_submitted";
    public const string Aborted = "aborted";
    public const string Unclear = "unclear";
    public const string NotAssessed = "not_assessed";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        None,
        TaskCardMismatch,
        MissingRequired,
        WrongFieldClass,
        WrongDependencyState,
        ValidationErrorOpen,
        NotSubmitted,
        Aborted,
        Unclear,
        NotAssessed,
    };

    public static IReadOnlySet<string> Emittable { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        None,
        TaskCardMismatch,
        MissingRequired,
        WrongFieldClass,
        WrongDependencyState,
        ValidationErrorOpen,
        NotSubmitted,
        NotAssessed,
    };
}
