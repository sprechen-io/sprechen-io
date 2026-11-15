using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Führt die freigegebene akustische Aktivierung aus und wechselt bei
/// klassifizierten Detektorfehlern zur Erkennung anhand von Text.
/// Ein erfolgreiches Aktivierungsergebnis wird an den Assistentenablauf gemeldet.
/// </summary>
internal sealed class WakeAcousticDetectionPath
{
    private readonly IWakeArbitrator _arbitrator;
    private readonly IAcousticWakeWordDetectorConfigurationResolver _configurationResolver;
    private readonly WakeListeningFallbackStatePublisher _fallbackStatePublisher;
    private readonly WakeCaptureUnavailableHandler _captureUnavailableHandler;

    public WakeAcousticDetectionPath(
        IWakeArbitrator arbitrator,
        IAcousticWakeWordDetectorConfigurationResolver configurationResolver,
        WakeListeningFallbackStatePublisher fallbackStatePublisher,
        WakeCaptureUnavailableHandler captureUnavailableHandler)
    {
        _arbitrator = arbitrator ?? throw new ArgumentNullException(nameof(arbitrator));
        _configurationResolver = configurationResolver ?? throw new ArgumentNullException(nameof(configurationResolver));
        _fallbackStatePublisher = fallbackStatePublisher ?? throw new ArgumentNullException(nameof(fallbackStatePublisher));
        _captureUnavailableHandler = captureUnavailableHandler ?? throw new ArgumentNullException(nameof(captureUnavailableHandler));
    }

    public async ValueTask<bool> TryActivateOrFallbackAsync(
        WakeRuntimeContext context,
        IWakeRuntimeOutcomeSink outcomeSink,
        string correlationId,
        WakeWordInventory wakeWordInventory,
        AcousticWakeWordDetectorConfiguration detectorConfiguration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(outcomeSink);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentNullException.ThrowIfNull(wakeWordInventory);
        ArgumentNullException.ThrowIfNull(detectorConfiguration);

        if (!detectorConfiguration.AllowsAcousticDetection)
        {
            return false;
        }

        var telemetry = context.WakeWordTelemetrySink;

        try
        {
            var acousticEvidence = await context.AcousticWakeWordTriggerPolicy
                .DetectAsync(
                    detectorConfiguration,
                    context.AcousticWakeWordScorer.ScoreAsync(detectorConfiguration, cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);

            await context.SpeechInteractionSignals.EnsureStoppedAsync().ConfigureAwait(false);
            var acousticOutcome = _arbitrator.Arbitrate(
                new WakeArbitrationInput(correlationId, wakeWordInventory)
                {
                    AcousticEvidence = acousticEvidence,
                });
            if (acousticOutcome is not WakeArbitrationOutcomeActivate
                {
                    Source: WakeWordActivationSource.Acoustic,
                    AcousticEvidence: { } activatedEvidence,
                })
            {
                throw new InvalidOperationException(
                    "WakeWordArbitrator returned a non-activate outcome for acoustic-evidence input.");
            }

            await telemetry
                .RecordAcousticDetectionAsync(
                    new WakeWordAcousticDetectionEvent(
                        correlationId,
                        WakeWordAcousticDetectionPhase.Detected,
                        activatedEvidence.DetectorId,
                        Exception: null),
                    WakeRuntimeTerminalCancellation.ForRequiredTerminalWork())
                .ConfigureAwait(false);
            await _captureUnavailableHandler
                .HandleRecoveryAsync(context, WakeRuntimeTerminalCancellation.ForRequiredTerminalWork())
                .ConfigureAwait(false);
            await outcomeSink
                .PublishAsync(
                    new WakeRuntimeAcousticDetectedOutcome(activatedEvidence, correlationId),
                    WakeRuntimeTerminalCancellation.ForRequiredTerminalWork())
                .ConfigureAwait(false);

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (AcousticWakeWordDetectorUnavailableException ex)
        {
            return await ApplyFallbackAsync(
                    context,
                    correlationId,
                    wakeWordInventory,
                    detectorConfiguration,
                    WakeWordAcousticDetectionPhase.DetectorUnavailable,
                    LexicalWakeFallbackReason.DetectorUnavailable,
                    ex)
                .ConfigureAwait(false);
        }
        catch (AcousticWakeWordDetectorWarmupException ex)
        {
            return await ApplyFallbackAsync(
                    context,
                    correlationId,
                    wakeWordInventory,
                    detectorConfiguration,
                    WakeWordAcousticDetectionPhase.DetectorWarmup,
                    LexicalWakeFallbackReason.DetectorWarmup,
                    ex)
                .ConfigureAwait(false);
        }
        catch (AcousticWakeWordDetectionFailedException ex)
        {
            return await ApplyFallbackAsync(
                    context,
                    correlationId,
                    wakeWordInventory,
                    detectorConfiguration,
                    WakeWordAcousticDetectionPhase.DetectorFailure,
                    LexicalWakeFallbackReason.DetectorFailure,
                    ex)
                .ConfigureAwait(false);
        }
    }

    private async ValueTask<bool> ApplyFallbackAsync(
        WakeRuntimeContext context,
        string correlationId,
        WakeWordInventory wakeWordInventory,
        AcousticWakeWordDetectorConfiguration detectorConfiguration,
        WakeWordAcousticDetectionPhase phase,
        LexicalWakeFallbackReason fallbackReason,
        Exception exception)
    {
        var telemetry = context.WakeWordTelemetrySink;
        await telemetry
            .RecordAcousticDetectionAsync(
                new WakeWordAcousticDetectionEvent(correlationId, phase, DetectorId: null, exception),
                WakeRuntimeTerminalCancellation.ForRequiredTerminalWork())
            .ConfigureAwait(false);

        var fallbackConfiguration = ApplyArbitratedFallback(
            correlationId,
            wakeWordInventory,
            detectorConfiguration,
            fallbackReason);

        await _fallbackStatePublisher
            .PublishAsync(context, fallbackConfiguration, correlationId, WakeRuntimeTerminalCancellation.ForRequiredTerminalWork())
            .ConfigureAwait(false);

        return false;
    }

    private AcousticWakeWordDetectorConfiguration ApplyArbitratedFallback(
        string correlationId,
        WakeWordInventory wakeWordInventory,
        AcousticWakeWordDetectorConfiguration detectorConfiguration,
        LexicalWakeFallbackReason fallbackReason)
    {
        var outcome = _arbitrator.Arbitrate(
            new WakeArbitrationInput(correlationId, wakeWordInventory)
            {
                FallbackReason = fallbackReason,
            });

        if (outcome is not WakeArbitrationOutcomeLexicalFallback fallback)
        {
            throw new InvalidOperationException(
                $"WakeWordArbitrator returned a non-fallback outcome '{outcome.GetType().Name}' for fallback-only input.");
        }

        return _configurationResolver.ResolveForLexicalFallback(fallback.FallbackReason, detectorConfiguration);
    }
}
