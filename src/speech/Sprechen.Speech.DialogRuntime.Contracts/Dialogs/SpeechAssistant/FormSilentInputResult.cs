using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Überträgt das Laufzeitergebnis einer stillen Eingabe für die Entscheidung zwischen Zustandsübernahme, Korrektur und erneuter Orientierung.
/// </summary>
public sealed class FormSilentInputResult
{
    private FormSilentInputResult(
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

    public static FormSilentInputResult Applied(
        SpeechAssistantApplicationSessionState applicationSession,
        bool canonicalSame,
        FormSurfaceSnapshot snapshot,
        FormSilentInputEligibilityProjection? eligibility)
    {
        ArgumentNullException.ThrowIfNull(applicationSession);
        ArgumentNullException.ThrowIfNull(snapshot);
        return new FormSilentInputResult(
            FormSilentInputResultKind.Applied,
            applicationSession,
            canonicalSame,
            snapshot,
            eligibility,
            repairKind: null);
    }

    public static FormSilentInputResult Rejected(FormSilentInputRepairKind repairKind)
        => new(
            FormSilentInputResultKind.Rejected,
            applicationSession: null,
            canonicalSame: false,
            snapshot: null,
            eligibility: null,
            repairKind);

    public static FormSilentInputResult StateChanged(
        FormSurfaceSnapshot snapshot,
        FormSilentInputEligibilityProjection? eligibility)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new FormSilentInputResult(
            FormSilentInputResultKind.StateChanged,
            applicationSession: null,
            canonicalSame: false,
            snapshot,
            eligibility,
            repairKind: null);
    }
}
