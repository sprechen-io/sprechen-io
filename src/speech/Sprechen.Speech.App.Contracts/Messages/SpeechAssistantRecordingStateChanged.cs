namespace Sprechen.Speech.App.Contracts.Messages;

/// <summary>
/// Meldet den Aufnahmestatus zur Darstellung der laufenden Mikrofonaufnahme.
/// </summary>
public sealed record SpeechAssistantRecordingStateChanged(bool IsRecording);

