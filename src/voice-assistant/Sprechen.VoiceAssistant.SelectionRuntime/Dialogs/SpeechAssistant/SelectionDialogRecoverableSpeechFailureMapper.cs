using Microsoft.Extensions.Logging;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.App.Runtime.Services;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant.Logging;

namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Übersetzt bekannte technische Aufnahme- und Erkennungsausnahmen in codierte Ergebnisse, damit die
/// Phasensteuerung deren Behandlung gemeinsam entscheiden kann.
/// </summary>
internal sealed class SelectionDialogRecoverableSpeechFailureMapper
{
    // Den gemeinsamen Fehlercode referenzieren, damit die Auswahl und andere Aufnahmeabläufe
    // denselben Abbruchgrund nicht unter getrennt gepflegten Zeichenketten melden.
    internal const string CaptureInterruptedFailureCode = SpeechCaptureFailureCodes.CaptureInterrupted;

    private readonly ILogger _logger;

    public SelectionDialogRecoverableSpeechFailureMapper(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public SpeechCaptureResult Map(SpeechRecognitionQueueFullException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return MapCore(exception, exception.Code);
    }

    public SpeechCaptureResult Map(SpeechRecognitionBackoffExhaustedException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return MapCore(exception, exception.Code);
    }

    public SpeechCaptureResult Map(OperationCanceledException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return MapCore(exception, CaptureInterruptedFailureCode);
    }

    private SpeechCaptureResult MapCore(Exception exception, string code)
    {
        _logger.LogWarning(SelectionRuntimeLogEvents.SpeechToTextRejected, exception, "Selection dialog speech-to-text rejected (code={Code}).", code);
        return new SpeechCaptureResult(null, null, code, true);
    }
}
