using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Gibt das Laufzeitergebnis einer stillen Eingabe an die Orchestrierung zurück, die eine aktualisierte Anwendungssitzung übernehmen kann.
/// </summary>
public sealed class FormSilentInputTransactionResult
{
    private FormSilentInputTransactionResult(
        FormSilentInputResultKind kind,
        SpeechAssistantApplicationSessionState? applicationSession,
        bool canonicalSame,
        FormSurfaceSnapshot? snapshot,
        FormSilentInputEligibilityProjection? eligibility,
        FormSilentInputRepairKind? repairKind)
    {
        Kind = kind;
        ApplicationSession = applicationSession;
        CanonicalSame = canonicalSame;
        Snapshot = snapshot;
        Eligibility = eligibility;
        RepairKind = repairKind;
    }

    public FormSilentInputResultKind Kind { get; }

    public SpeechAssistantApplicationSessionState? ApplicationSession { get; }

    public bool CanonicalSame { get; }

    public FormSurfaceSnapshot? Snapshot { get; }

    public FormSilentInputEligibilityProjection? Eligibility { get; }

    public FormSilentInputRepairKind? RepairKind { get; }

    public static FormSilentInputTransactionResult Applied(
        SpeechAssistantApplicationSessionState applicationSession,
        bool canonicalSame,
        FormSurfaceSnapshot snapshot,
        FormSilentInputEligibilityProjection? eligibility)
    {
        ArgumentNullException.ThrowIfNull(applicationSession);
        ArgumentNullException.ThrowIfNull(snapshot);
        return new FormSilentInputTransactionResult(
            FormSilentInputResultKind.Applied,
            applicationSession,
            canonicalSame,
            snapshot,
            eligibility,
            repairKind: null);
    }

    public static FormSilentInputTransactionResult Rejected(FormSilentInputRepairKind repairKind)
        => new(
            FormSilentInputResultKind.Rejected,
            applicationSession: null,
            canonicalSame: false,
            snapshot: null,
            eligibility: null,
            repairKind);

    public static FormSilentInputTransactionResult StateChanged(
        FormSurfaceSnapshot snapshot,
        FormSilentInputEligibilityProjection? eligibility)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new FormSilentInputTransactionResult(
            FormSilentInputResultKind.StateChanged,
            applicationSession: null,
            canonicalSame: false,
            snapshot,
            eligibility,
            repairKind: null);
    }

    public static FormSilentInputTransactionResult FromRuntime(FormSilentInputResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Kind switch
        {
            FormSilentInputResultKind.Applied => Applied(
                result.ApplicationSession!,
                result.CanonicalSame,
                result.Snapshot!,
                result.Eligibility),
            FormSilentInputResultKind.Rejected => Rejected(result.RepairKind!.Value),
            FormSilentInputResultKind.StateChanged => StateChanged(result.Snapshot!, result.Eligibility),
            _ => throw new InvalidOperationException("Unknown silent-input result kind.")
        };
    }
}
