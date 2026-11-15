using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Gibt Abschluss, Navigation oder Unterbrechung samt Zustandsdaten an die übergeordnete Assistentensteuerung zurück.
/// </summary>
public sealed record VoiceCommandDialogResult
{
    public bool WasSubmitted => Outcome == EvaluationOutcome.Submitted;

    public bool WasCancelled => Outcome == EvaluationOutcome.Aborted && AbortReason == EvaluationAbortReason.Cancelled;

    public bool WasAbandoned => Outcome == EvaluationOutcome.Aborted && AbortReason == EvaluationAbortReason.Abandoned;

    public bool WasPaused { get; init; }

    public EvaluationOutcome Outcome { get; init; }

    public EvaluationAbortReason? AbortReason { get; init; }

    public string? ApplicationId { get; init; }

    public string? FormId { get; init; }

    public VoiceCommandNavigationKind Navigation { get; init; }

    public string? NavigationFormId { get; init; }

    public string? NavigationLanguageIsoCode { get; init; }

    public PolitenessLevel? NavigationPolitenessLevel { get; init; }

    public bool IsSilentInputRequested { get; init; }

    public FormSilentInputEligibilityProjection? SilentInputEligibility { get; init; }

    public ImmutableDictionary<string, string> ValuesByVariableId { get; init; } =
        ImmutableDictionary.Create<string, string>(StringComparer.Ordinal);

    private VoiceCommandDialogResult()
    {
    }

    public static VoiceCommandDialogResult Submitted(
        string applicationId,
        string formId,
        ImmutableDictionary<string, string> valuesByVariableId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(formId);
        ArgumentNullException.ThrowIfNull(valuesByVariableId);

        return new VoiceCommandDialogResult
        {
            Outcome = EvaluationOutcome.Submitted,
            ApplicationId = applicationId.Trim(),
            FormId = formId.Trim(),
            ValuesByVariableId = valuesByVariableId
        };
    }

    public static VoiceCommandDialogResult Cancelled(
        string applicationId,
        string? formId,
        ImmutableDictionary<string, string> valuesByVariableId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        ArgumentNullException.ThrowIfNull(valuesByVariableId);

        return new VoiceCommandDialogResult
        {
            Outcome = EvaluationOutcome.Aborted,
            AbortReason = EvaluationAbortReason.Cancelled,
            ApplicationId = applicationId.Trim(),
            FormId = string.IsNullOrWhiteSpace(formId) ? null : formId.Trim(),
            ValuesByVariableId = valuesByVariableId
        };
    }

    public static VoiceCommandDialogResult Abandoned(
        string applicationId,
        string? formId,
        ImmutableDictionary<string, string> valuesByVariableId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        ArgumentNullException.ThrowIfNull(valuesByVariableId);

        return new VoiceCommandDialogResult
        {
            Outcome = EvaluationOutcome.Aborted,
            AbortReason = EvaluationAbortReason.Abandoned,
            ApplicationId = applicationId.Trim(),
            FormId = string.IsNullOrWhiteSpace(formId) ? null : formId.Trim(),
            ValuesByVariableId = valuesByVariableId
        };
    }

    public static VoiceCommandDialogResult Paused(
        string applicationId,
        string formId,
        ImmutableDictionary<string, string> valuesByVariableId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(formId);
        ArgumentNullException.ThrowIfNull(valuesByVariableId);

        return new VoiceCommandDialogResult
        {
            Outcome = EvaluationOutcome.Aborted,
            AbortReason = EvaluationAbortReason.Abandoned,
            WasPaused = true,
            ApplicationId = applicationId.Trim(),
            FormId = formId.Trim(),
            ValuesByVariableId = valuesByVariableId
        };
    }

    public static VoiceCommandDialogResult AbandonedWithoutContext(
        ImmutableDictionary<string, string> valuesByVariableId)
    {
        ArgumentNullException.ThrowIfNull(valuesByVariableId);

        return new VoiceCommandDialogResult
        {
            Outcome = EvaluationOutcome.Aborted,
            AbortReason = EvaluationAbortReason.Abandoned,
            ValuesByVariableId = valuesByVariableId
        };
    }

    public static VoiceCommandDialogResult BackToFormSelection(
        string applicationId,
        string? formId,
        ImmutableDictionary<string, string> valuesByVariableId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        ArgumentNullException.ThrowIfNull(valuesByVariableId);

        return new VoiceCommandDialogResult
        {
            Outcome = EvaluationOutcome.Aborted,
            AbortReason = EvaluationAbortReason.Abandoned,
            ApplicationId = applicationId.Trim(),
            FormId = string.IsNullOrWhiteSpace(formId) ? null : formId.Trim(),
            Navigation = VoiceCommandNavigationKind.BackToFormSelection,
            ValuesByVariableId = valuesByVariableId
        };
    }

    public static VoiceCommandDialogResult BackToAppSelection(
        string applicationId,
        string? formId,
        ImmutableDictionary<string, string> valuesByVariableId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        ArgumentNullException.ThrowIfNull(valuesByVariableId);

        return new VoiceCommandDialogResult
        {
            Outcome = EvaluationOutcome.Aborted,
            AbortReason = EvaluationAbortReason.Abandoned,
            ApplicationId = applicationId.Trim(),
            FormId = string.IsNullOrWhiteSpace(formId) ? null : formId.Trim(),
            Navigation = VoiceCommandNavigationKind.BackToAppSelection,
            ValuesByVariableId = valuesByVariableId
        };
    }

    public static VoiceCommandDialogResult SwitchForm(
        string applicationId,
        string? fromFormId,
        string toFormId,
        ImmutableDictionary<string, string> valuesByVariableId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(toFormId);
        ArgumentNullException.ThrowIfNull(valuesByVariableId);

        return new VoiceCommandDialogResult
        {
            Outcome = EvaluationOutcome.Aborted,
            AbortReason = EvaluationAbortReason.Abandoned,
            ApplicationId = applicationId.Trim(),
            FormId = string.IsNullOrWhiteSpace(fromFormId) ? null : fromFormId.Trim(),
            Navigation = VoiceCommandNavigationKind.SwitchForm,
            NavigationFormId = toFormId.Trim(),
            ValuesByVariableId = valuesByVariableId
        };
    }

    public static VoiceCommandDialogResult ChangeLanguage(
        string applicationId,
        string? fromFormId,
        string targetIsoCode,
        ImmutableDictionary<string, string> valuesByVariableId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetIsoCode);
        ArgumentNullException.ThrowIfNull(valuesByVariableId);

        var normalizedIsoCode = IsoCode.TryNormalizeIso6391(targetIsoCode)
            ?? throw new ArgumentException("Expected ISO 639-1 language code.", nameof(targetIsoCode));

        return new VoiceCommandDialogResult
        {
            Outcome = EvaluationOutcome.Aborted,
            AbortReason = EvaluationAbortReason.Abandoned,
            ApplicationId = applicationId.Trim(),
            FormId = string.IsNullOrWhiteSpace(fromFormId) ? null : fromFormId.Trim(),
            Navigation = VoiceCommandNavigationKind.ChangeLanguage,
            NavigationLanguageIsoCode = normalizedIsoCode,
            ValuesByVariableId = valuesByVariableId
        };
    }

    public static VoiceCommandDialogResult SilentInputRequested(
        FormSilentInputEligibilityProjection projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentException.ThrowIfNullOrWhiteSpace(projection.FieldId);

        return new VoiceCommandDialogResult
        {
            Outcome = EvaluationOutcome.Aborted,
            AbortReason = EvaluationAbortReason.Abandoned,
            WasPaused = true,
            IsSilentInputRequested = true,
            SilentInputEligibility = projection
        };
    }

    public static VoiceCommandDialogResult ChangePoliteness(
        string applicationId,
        string? fromFormId,
        PolitenessLevel targetPolitenessLevel,
        ImmutableDictionary<string, string> valuesByVariableId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        ArgumentNullException.ThrowIfNull(valuesByVariableId);

        return new VoiceCommandDialogResult
        {
            Outcome = EvaluationOutcome.Aborted,
            AbortReason = EvaluationAbortReason.Abandoned,
            ApplicationId = applicationId.Trim(),
            FormId = string.IsNullOrWhiteSpace(fromFormId) ? null : fromFormId.Trim(),
            Navigation = VoiceCommandNavigationKind.ChangePoliteness,
            NavigationPolitenessLevel = targetPolitenessLevel,
            ValuesByVariableId = valuesByVariableId
        };
    }
}
