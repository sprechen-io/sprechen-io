using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Sprachaufnahme im Formulardialog mit Wiederholungen oder einzelnen Versuchen.
/// Ein Cursor des Audiorings ermöglicht es, Antworten aus der Pause nach einer
/// Ansage in die anschließende Erfassung einzubeziehen.
/// </summary>
public interface IFormVoiceCommandRecordingService
{
    Task<CapturedSpeechAudio> RecordUntilSpeechAsync(
        SpeechCaptureMode captureMode,
        VoiceDialogSettings voiceSettings,
        CancellationToken cancellationToken)
        => RecordUntilSpeechAsync(captureMode, voiceSettings, startCursor: null, cancellationToken);

    /// <summary>
    /// Nimmt bis zur Sprache auf und verarbeitet zunächst bereits gepufferte Audiodaten
    /// ab <paramref name="startCursor"/>. Der Dialogpfad ohne Barge-in erfasst den Cursor
    /// am Promptende, damit Antworten während der anschließenden Pause erhalten bleiben.
    /// <c>null</c> verwendet die Erfassung ohne Startcursor.
    /// </summary>
    Task<CapturedSpeechAudio> RecordUntilSpeechAsync(
        SpeechCaptureMode captureMode,
        VoiceDialogSettings voiceSettings,
        SampleCursor? startCursor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Führt genau einen Erfassungsversuch aus und meldet dem Beobachter den
    /// übergebenen <paramref name="attempt"/>-Kontext. Fehlende Eingabe wird als
    /// <see cref="NoSpeechDetectedException"/> gemeldet. Aufrufer mit eigener
    /// Wiederholungsschleife behalten dadurch ihre Versuchsnummer und ihr Budget.
    /// </summary>
    Task<CapturedSpeechAudio> RecordSingleAttemptAsync(
        SpeechCaptureMode captureMode,
        VoiceDialogSettings voiceSettings,
        SampleCursor? startCursor,
        FormVoiceCommandAttempt attempt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Liefert die aktuelle Schreibposition des Audiorings oder <c>null</c>, wenn
    /// der Client keinen Ringcursor bereitstellt. Aufrufer erfassen den Cursor vor
    /// einer Pause und übergeben ihn anschließend an
    /// <see cref="RecordUntilSpeechAsync(SpeechCaptureMode, VoiceDialogSettings, SampleCursor?, CancellationToken)"/>.
    /// </summary>
    SampleCursor? SnapshotLiveCaptureCursor() => null;
}

