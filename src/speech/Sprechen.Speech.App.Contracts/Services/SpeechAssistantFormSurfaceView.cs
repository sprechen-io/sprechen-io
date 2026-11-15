using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Verbindet den Formularschnappschuss mit Interaktionszustand und optionaler Aktionsidentität für die Oberfläche.
/// </summary>
public sealed record SpeechAssistantFormSurfaceView(
    FormSurfaceSnapshot Snapshot,
    SpeechAssistantFormSurfaceInteractionState Interaction,
    SpeechAssistantSilentInputSnapshotIdentity? ActionIdentity)
{
    public static SpeechAssistantFormSurfaceView FromSafeSnapshot(FormSurfaceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new SpeechAssistantFormSurfaceView(
            snapshot,
            new SpeechAssistantFormSurfaceInteractionState(
                SpeechAssistantSilentInputPhase.Unavailable,
                FieldId: null,
                ControlKind: null,
                OriginSurfaceRevision: 0,
                RepairKind: null),
            ActionIdentity: null);
    }
}
