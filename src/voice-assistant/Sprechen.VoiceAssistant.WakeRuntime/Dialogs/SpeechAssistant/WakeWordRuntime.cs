namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Richtet die Aktivierungserkennung ein und startet deren akustischen
/// oder textbasierten Ablauf über den gemeinsamen Koordinator.
/// </summary>
public sealed class WakeWordRuntime
{
    private readonly LexicalWakeListenerCoordinator _coordinator;
    private readonly WakeListeningFallbackStatePublisher _fallbackStatePublisher;

    internal WakeWordRuntime(
        LexicalWakeListenerCoordinator coordinator,
        WakeListeningFallbackStatePublisher fallbackStatePublisher)
    {
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _fallbackStatePublisher = fallbackStatePublisher ?? throw new ArgumentNullException(nameof(fallbackStatePublisher));
    }

    public async ValueTask RunAsync(
        string selectedIsoCode,
        WakeRuntimeContext context,
        IWakeRuntimeOutcomeSink outcomeSink,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedIsoCode);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(outcomeSink);

        var correlationId = Guid.NewGuid().ToString("N");
        var telemetry = context.WakeWordTelemetrySink;

        await telemetry
            .RecordListenerSetupAsync(
                new WakeWordListenerSetupEvent(
                    correlationId,
                    WakeWordListenerSetupPhase.Started,
                    selectedIsoCode,
                    ActivationFormCount: null,
                    RecognitionOnlyActivationFormCount: null,
                    Exception: null),
                cancellationToken)
            .ConfigureAwait(false);

        WakeWordInventory wakeWordInventory;
        AcousticWakeWordDetectorConfiguration detectorConfiguration;
        try
        {
            wakeWordInventory = await context.WakeWordProvider
                .GetWakeWordInventoryAsync(selectedIsoCode, cancellationToken)
                .ConfigureAwait(false);
            detectorConfiguration = await context.DetectorConfigurationResolver
                .ResolveAsync(selectedIsoCode, wakeWordInventory, cancellationToken)
                .ConfigureAwait(false);
            await telemetry
                .RecordListenerSetupAsync(
                    new WakeWordListenerSetupEvent(
                        correlationId,
                        WakeWordListenerSetupPhase.InventoryLoaded,
                        IsoCode: null,
                        wakeWordInventory.ActivationForms.Count,
                        wakeWordInventory.RecognitionOnlyActivationForms.Count,
                        Exception: null),
                    cancellationToken)
                .ConfigureAwait(false);
            await _fallbackStatePublisher
                .PublishAsync(context, detectorConfiguration, correlationId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await telemetry
                .RecordListenerSetupAsync(
                    new WakeWordListenerSetupEvent(
                        correlationId,
                        WakeWordListenerSetupPhase.Failed,
                        IsoCode: null,
                        ActivationFormCount: null,
                        RecognitionOnlyActivationFormCount: null,
                        ex),
                    WakeRuntimeTerminalCancellation.ForRequiredTerminalWork())
                .ConfigureAwait(false);
            await outcomeSink
                .PublishAsync(new WakeRuntimeListenerFailedOutcome(), WakeRuntimeTerminalCancellation.ForRequiredTerminalWork())
                .ConfigureAwait(false);
            return;
        }

        var parameters = new LexicalWakeListenerRunParameters(
            selectedIsoCode,
            correlationId,
            wakeWordInventory,
            detectorConfiguration);

        await _coordinator.RunAsync(context, outcomeSink, parameters, cancellationToken).ConfigureAwait(false);
    }
}
