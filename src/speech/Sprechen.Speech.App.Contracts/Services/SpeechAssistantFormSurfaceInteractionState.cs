namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Bündelt Phase, Feldbezug und Korrekturbedarf der stillen Eingabe für die Formularansicht.
/// </summary>
public sealed record SpeechAssistantFormSurfaceInteractionState(
    SpeechAssistantSilentInputPhase Phase,
    string? FieldId,
    SpeechAssistantSilentInputControlKind? ControlKind,
    long OriginSurfaceRevision,
    FormSilentInputRepairKind? RepairKind);
