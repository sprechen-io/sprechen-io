using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.App.Runtime.Services;

namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Koordiniert Aufnahme, Transkription und Aktivierung anhand des erkannten
/// Texts sowie das anschließende Aufräumen. Zuvor kann die akustische
/// Erkennung den Assistenten bereits aktivieren.
/// </summary>
internal sealed class LexicalWakeListenerCoordinator
{
    private readonly IWakeArbitrator _arbitrator;
    private readonly WakeAcousticDetectionPath _acousticDetectionPath;
    private readonly WakeWordLexicalActivationDispatcher _activationDispatcher;
    private readonly WakeWordMissOutcomeHandler _missOutcomeHandler;
    private readonly WakeRecoveryGuidanceGate _guidanceGate;
    private readonly IBackoffPolicy _captureFailureBackoff;
    private readonly IWakeCaptureRetryPacer _captureRetryPacer;
    private readonly MicrophoneAvailabilityGate _microphoneAvailabilityGate;
    private readonly WakeCaptureUnavailableHandler _captureUnavailableHandler;

    public LexicalWakeListenerCoordinator(
        IWakeArbitrator arbitrator,
        WakeAcousticDetectionPath acousticDetectionPath,
        WakeWordLexicalActivationDispatcher activationDispatcher,
        WakeWordMissOutcomeHandler missOutcomeHandler,
        WakeRecoveryGuidanceGate guidanceGate,
        IBackoffPolicy captureFailureBackoff,
        IWakeCaptureRetryPacer captureRetryPacer,
        MicrophoneAvailabilityGate microphoneAvailabilityGate,
        WakeCaptureUnavailableHandler captureUnavailableHandler)
    {
        _arbitrator = arbitrator ?? throw new ArgumentNullException(nameof(arbitrator));
        _acousticDetectionPath = acousticDetectionPath ?? throw new ArgumentNullException(nameof(acousticDetectionPath));
        _activationDispatcher = activationDispatcher ?? throw new ArgumentNullException(nameof(activationDispatcher));
        _missOutcomeHandler = missOutcomeHandler ?? throw new ArgumentNullException(nameof(missOutcomeHandler));
        _guidanceGate = guidanceGate ?? throw new ArgumentNullException(nameof(guidanceGate));
        _captureFailureBackoff = captureFailureBackoff ?? throw new ArgumentNullException(nameof(captureFailureBackoff));
        _captureRetryPacer = captureRetryPacer ?? throw new ArgumentNullException(nameof(captureRetryPacer));
        _microphoneAvailabilityGate = microphoneAvailabilityGate ?? throw new ArgumentNullException(nameof(microphoneAvailabilityGate));
        _captureUnavailableHandler = captureUnavailableHandler ?? throw new ArgumentNullException(nameof(captureUnavailableHandler));
    }

    public async ValueTask RunAsync(
        WakeRuntimeContext context,
        IWakeRuntimeOutcomeSink outcomeSink,
        LexicalWakeListenerRunParameters parameters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(outcomeSink);
        ArgumentNullException.ThrowIfNull(parameters);

        if (string.IsNullOrWhiteSpace(parameters.SelectedIsoCode))
        {
            throw new ArgumentException(
                "LexicalWakeListenerRunParameters.SelectedIsoCode must be a non-empty string.",
                nameof(parameters));
        }

        if (string.IsNullOrWhiteSpace(parameters.CorrelationId))
        {
            throw new ArgumentException(
                "LexicalWakeListenerRunParameters.CorrelationId must be a non-empty string.",
                nameof(parameters));
        }

        ArgumentNullException.ThrowIfNull(parameters.Inventory);
        ArgumentNullException.ThrowIfNull(parameters.DetectorConfiguration);

        var correlationId = parameters.CorrelationId;
        var selectedIsoCode = parameters.SelectedIsoCode;
        var wakeWordInventory = parameters.Inventory;
        var detectorConfiguration = parameters.DetectorConfiguration;

        var telemetry = context.WakeWordTelemetrySink;
        var wakeUtteranceCapturePhase = new WakeUtteranceCapturePhase(telemetry, correlationId);
        var wakeTranscriptionPhase = new WakeTranscriptionPhase(
            context.SpeechRecognitionWorker,
            outcomeSink,
            telemetry,
            correlationId);
        var wakeLexicalOutcomeResolver = new WakeLexicalOutcomeResolver(_arbitrator, _missOutcomeHandler);
        var wakeListeningTeardown = new WakeListeningTeardown();

        _microphoneAvailabilityGate.BeginListeningRun();

        try
        {
            var acousticActivated = await _acousticDetectionPath
                .TryActivateOrFallbackAsync(
                    context,
                    outcomeSink,
                    correlationId,
                    wakeWordInventory,
                    detectorConfiguration,
                    cancellationToken)
                .ConfigureAwait(false);
            if (acousticActivated)
            {
                // Auch eine akustische Aktivierung sperrt weitere Erkennungshinweise
                // für die Lebensdauer dieser Instanz, ebenso wie eine Aktivierung anhand von Text.
                _guidanceGate.RecordWake();
                _microphoneAvailabilityGate.RecordWake();
                return;
            }

            var segmentStreamSource = context.SegmentStreamSource;
            // Der Start der fortlaufenden Segmentquelle gehört zur Audioerfassung.
            // Ein dabei klassifizierter Mikrofonfehler nimmt denselben Hinweis- und
            // Wiederholungsweg wie ein Ausfall während einer späteren Erfassung.
            var listening = false;

            var consecutiveCaptureFailures = 0;
            // Transkriptionsfehler brauchen einen eigenen Serienfehlerzähler: Eine
            // erfolgreiche Audioerfassung setzt den Erfassungszähler zurück, bevor STT läuft.
            // Ein gemeinsamer Zähler würde deshalb bei dauerhaft scheiternder Erkennung
            // die ansteigende Wartezeit in jedem Durchlauf wieder von vorn beginnen lassen.
            var consecutiveTranscriptionFailures = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                var captureOutcome = listening
                    ? await wakeUtteranceCapturePhase.CaptureAsync(
                            segmentStreamSource,
                            cancellationToken)
                        .ConfigureAwait(false)
                    : await wakeUtteranceCapturePhase.BeginListeningAsync(
                            segmentStreamSource,
                            cancellationToken)
                        .ConfigureAwait(false);

                if (captureOutcome.Failed)
                {
                    // Bei klassifiziertem Mikrofonfehler kann der Handler einen Hinweis anfordern.
                    // Fehlerschwelle und begrenzter wachsender Abstand bestimmen dessen Freigabe.
                    // Die Klassifikation bleibt auch ohne Ansage erhalten.
                    var announced = await _captureUnavailableHandler
                        .HandleAsync(context, captureOutcome.UnavailableReason, cancellationToken)
                        .ConfigureAwait(false);

                    // Sofort scheiternde Erfassungen würden ohne Wartezeit eine enge
                    // Wiederholungsschleife erzeugen. Der exponentielle Backoff begrenzt diese
                    // Last. Ein erfasstes Segment oder ein regulärer Ablauf ohne Sprache setzt
                    // den Erfassungszähler zurück. Die nächste Fehlerfolge beginnt dann neu.
                    var retryDelay = _captureFailureBackoff.GetDelay(consecutiveCaptureFailures);
                    // Die ursachenspezifische Mindestpause gilt erst ab der Hinweisschwelle.
                    // Davor bleiben kürzere Wiederholungen möglich. Bei verweigerter Berechtigung
                    // begrenzt danach der Basisabstand weitere Anfragen.
                    var guidance = context.MicrophoneAvailabilityGuidance;
                    var toldOrDue = announced || consecutiveCaptureFailures + 1 >= guidance.UnavailableThreshold;
                    var retryFloor = toldOrDue ? guidance.RetryFloor(captureOutcome.UnavailableReason) : TimeSpan.Zero;
                    if (retryFloor > retryDelay)
                    {
                        retryDelay = retryFloor;
                    }

                    await _captureRetryPacer
                        .DelayAsync(retryDelay, cancellationToken)
                        .ConfigureAwait(false);
                    consecutiveCaptureFailures++;
                    continue;
                }

                if (!listening)
                {
                    // Die Quelle hat den Start angenommen. Erst ein tatsächlicher Erfassungszyklus
                    // entscheidet, ob ein zuvor angekündigter Ausfall behoben ist.
                    listening = true;
                    continue;
                }

                consecutiveCaptureFailures = 0;
                // Nach regulärer Erfassung gegebenenfalls Entwarnung anfordern.
                // Erst nach erfolgreicher Rückkehr der Wiedergabe wird der Zustand bestätigt.
                await _captureUnavailableHandler
                    .HandleRecoveryAsync(context, cancellationToken)
                    .ConfigureAwait(false);

                var utterance = captureOutcome.Audio;
                if (utterance is null)
                {
                    continue;
                }

                // Den Zeitanker vor der Transkription für die spätere Segmentzusammenführung
                // erfassen. Eine gegebenenfalls notwendige Entwarnung ist hier bereits abgearbeitet.
                var capturedAt = DateTimeOffset.UtcNow;

                var transcriptionOutcome = await wakeTranscriptionPhase.TranscribeAsync(
                        utterance,
                        selectedIsoCode,
                        wakeWordInventory,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (transcriptionOutcome.Disposition == WakeTranscriptionDisposition.Rejected)
                {
                    return;
                }

                if (transcriptionOutcome.Disposition == WakeTranscriptionDisposition.Faulted)
                {
                    // Bei gescheiterter Transkription steuert deren eigener Fehlerzähler die Pause.
                    // Die Audioerfassung war erfolgreich: Eine Mikrofon-Ausfallansage würde
                    // hier eine andere Ursache behaupten und wird deshalb nicht ausgelöst.
                    await _captureRetryPacer
                        .DelayAsync(_captureFailureBackoff.GetDelay(consecutiveTranscriptionFailures), cancellationToken)
                        .ConfigureAwait(false);
                    consecutiveTranscriptionFailures++;
                    continue;
                }

                consecutiveTranscriptionFailures = 0;

                if (transcriptionOutcome.Disposition == WakeTranscriptionDisposition.NoSpeech)
                {
                    continue;
                }

                if (!transcriptionOutcome.TryGetRecognized(out var transcript))
                {
                    // Ergebnisart ohne verfügbares Transkript: Der Transkriptionsfehlerzähler
                    // steigt, der nächste Zyklus beginnt ohne zusätzliche Wartezeit.
                    consecutiveTranscriptionFailures++;
                    continue;
                }

                var (activation, continueListening) = await wakeLexicalOutcomeResolver.ResolveAsync(
                        context,
                        outcomeSink,
                        telemetry,
                        correlationId,
                        wakeWordInventory,
                        transcript,
                        capturedAt,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (continueListening)
                {
                    continue;
                }

                if (activation is null)
                {
                    throw new InvalidOperationException("Lexical wake outcome did not return an activation or a continuation.");
                }

                // Nach der ersten Aktivierung anhand von Text weitere gesprochene
                // Erkennungshinweise über diese Instanz sperren.
                _guidanceGate.RecordWake();
                _microphoneAvailabilityGate.RecordWake();
                await _activationDispatcher
                    .DispatchAsync(context, outcomeSink, telemetry, correlationId, activation, wakeWordInventory)
                    .ConfigureAwait(false);
                return;
            }
        }
        finally
        {
            await wakeListeningTeardown
                .StopAsync(context.SegmentStreamSource)
                .ConfigureAwait(false);
            wakeLexicalOutcomeResolver.Reset();
        }
    }

}
