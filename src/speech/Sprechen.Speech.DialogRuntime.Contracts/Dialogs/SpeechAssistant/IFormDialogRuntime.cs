namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt Zustand, Navigation und Eingabeverarbeitung als Laufzeitvertrag für die Steuerung eines Formulardialogs.
/// </summary>
public interface IFormDialogRuntime :
    IFormDialogSurfaceState,
    IFormDialogTrustedValueState,
    IFormDialogRuntimeState,
    IFormDialogRuntimeNavigation,
    IFormDialogValueCaptureInput,
    IFormDialogInputModePolicyInput,
    IFormDialogValidationInput
{
    IFormDialogSilentInput SilentInput { get; }
}
