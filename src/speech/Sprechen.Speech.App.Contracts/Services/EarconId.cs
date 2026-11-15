namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Kennungen der Hinweistöne. Die Zahlen 2 und 7 bleiben frei,
/// damit früher gespeicherte Werte keinem anderen Hinweiston zugeordnet werden.
/// </summary>
public enum EarconId
{
    FlowOutputStart = 0,
    FlowListening = 1,
    FlowAck = 3,
    FlowProcessing = 4,
    FlowBargeIn = 5,
    FlowDone = 6,
    ValidationSuccess = 8,
    ValidationAsrFail = 9,
    ValidationAmbiguous = 10,
    StructureStepTransition = 11,
    StructureBoundary = 12,
    ModePrivacyChange = 13,
    ModeConfirmationRequired = 14,
    VoiceInputMicOn = 15,
    VoiceInputMicOff = 16,
    WakeWordNotRecognized = 17
}
