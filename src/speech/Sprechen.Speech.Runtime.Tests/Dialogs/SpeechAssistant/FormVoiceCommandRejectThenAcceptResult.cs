using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

internal sealed record FormVoiceCommandRejectThenAcceptResult(
    VoiceCommandDialogResult DialogResult,
    RecordingEvaluationTelemetry Telemetry,
    RecordingTextToSpeechPlayer TextToSpeech);
