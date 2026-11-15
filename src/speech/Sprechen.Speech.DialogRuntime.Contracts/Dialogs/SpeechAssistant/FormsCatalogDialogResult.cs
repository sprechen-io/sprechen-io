namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ergebnis des Anwendungskatalogdialogs mit ausgewählter Anwendung oder Abbruchgrund
/// für die Fortsetzung des Sitzungsablaufs.
/// </summary>
public sealed record FormsCatalogDialogResult
{
    public bool WasCancelled { get; init; }

    public DialogCancellationReason? CancellationReason { get; init; }

    public string? EffectiveIsoCode { get; init; }

    public string? SelectedApplicationId { get; init; }

    public string? SelectedApplicationName { get; init; }

    /// <summary>
    /// Zahl der Formulare in der ausgewählten Anwendung. Null bei einem Abbruch.
    /// </summary>
    public int? FormCount { get; init; }

    public string? DefaultFormId { get; init; }

    public bool HasMultipleForms => FormCount.HasValue && FormCount.Value > 1;

    private FormsCatalogDialogResult()
    {
    }

    public static FormsCatalogDialogResult Cancelled(DialogCancellationReason reason, string? effectiveIsoCode = null) => new()
    {
        WasCancelled = true,
        CancellationReason = reason,
        EffectiveIsoCode = effectiveIsoCode
    };

    public static FormsCatalogDialogResult Selected(
        string applicationId,
        string applicationName,
        int formCount,
        string? defaultFormId,
        string? effectiveIsoCode = null) => new()
    {
        WasCancelled = false,
        CancellationReason = null,
        EffectiveIsoCode = effectiveIsoCode,
        SelectedApplicationId = applicationId,
        SelectedApplicationName = applicationName,
        FormCount = formCount,
        DefaultFormId = defaultFormId
    };
}

