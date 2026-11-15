namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Beschreibt den Wiederholungs- oder Endübergang für einen eingeordneten Aufnahmefehler und bindet
/// seine Übernahme an den erwarteten bisherigen Fehlerzähler.
/// </summary>
/// <param name="Kind">Der von der Regel bestimmte Wiederholungs- oder Endübergang.</param>
/// <param name="ExpectedCurrentCount">Der bei Übernahme noch erwartete Fehlerzähler.</param>
public sealed record SelectionCaptureFailureTransitionDecision(
    SelectionCaptureFailureTransitionKind Kind,
    int ExpectedCurrentCount);
