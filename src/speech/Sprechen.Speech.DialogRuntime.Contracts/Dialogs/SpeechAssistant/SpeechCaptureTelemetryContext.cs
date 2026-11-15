namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet eine Sprachaufnahme ihrer Evaluationssitzung und gegebenenfalls einer unterbrochenen Ansage zu.
/// </summary>
public sealed record SpeechCaptureTelemetryContext(
    EvaluationSession Session,
    string? InterruptedPromptId = null);
