namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Ordnet bekannte Rückmeldungsklänge ihrer funktionalen Familie zu und weist unbekannte Kennungen zurück.
/// </summary>
public static class EarconIdExtensions
{
    public static EarconFamily GetFamily(this EarconId earcon)
    {
        return earcon switch
        {
            EarconId.FlowOutputStart
                or EarconId.FlowListening
                or EarconId.FlowAck
                or EarconId.FlowProcessing
                or EarconId.FlowBargeIn
                or EarconId.FlowDone => EarconFamily.Flow,

            EarconId.ValidationSuccess => EarconFamily.ValidationSuccess,

            EarconId.ValidationAsrFail
                or EarconId.ValidationAmbiguous
                or EarconId.WakeWordNotRecognized => EarconFamily.ValidationNotOk,

            EarconId.StructureStepTransition
                or EarconId.StructureBoundary => EarconFamily.Structure,

            EarconId.ModePrivacyChange
                or EarconId.ModeConfirmationRequired
                or EarconId.VoiceInputMicOn
                or EarconId.VoiceInputMicOff => EarconFamily.Mode,

            _ => throw new ArgumentOutOfRangeException(nameof(earcon), earcon, "Unknown earcon id.")
        };
    }
}

