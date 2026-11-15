namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt Formularinterpreter, Evaluationssitzung und Zustandsverfolgung für globale Sprachbefehle.
/// </summary>
public sealed record FormVoiceCommandGlobalCommandRuntime(
    IFormDialogRuntime Interpreter,
    EvaluationSession Session,
    IFormVoiceCommandDialogStateTracker StateTracker);
