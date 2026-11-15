using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Veröffentlicht den Zustand der textbasierten Erkennung als Ersatz
/// für den akustischen Detektor und meldet zugehörige Telemetrie.
/// Wird beim Start sowie beim späteren Wechsel auf diesen Ersatz aufgerufen.
/// </summary>
internal sealed class WakeListeningFallbackStatePublisher
{
    public async ValueTask PublishAsync(
        WakeRuntimeContext context,
        AcousticWakeWordDetectorConfiguration configuration,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(configuration);

        var telemetry = context.WakeWordTelemetrySink;

        LexicalWakeFallbackReasonExtensions.TryFromCatalogReasonCode(
            configuration.LexicalFallbackReasonCode,
            configuration.DetectorProfile.FallbackReasonCodes,
            out var reason);

        var state = configuration.RequiresLexicalFallback
            ? new SpeechWakeListeningFallbackState(true, configuration.LexicalFallbackReasonCode, reason)
            : SpeechWakeListeningFallbackState.Inactive;

        try
        {
            await context.SpeechInteractionSignals
                .SetWakeListeningFallbackStateAsync(state)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await telemetry
                .RecordFallbackAsync(
                    new WakeWordFallbackEvent(correlationId, WakeWordFallbackPhase.PublishFailed, ReasonCode: null, ex),
                    WakeRuntimeTerminalCancellation.ForRequiredTerminalWork())
                .ConfigureAwait(false);
        }

        if (!state.IsLexicalFallbackActive)
        {
            return;
        }

        await telemetry
            .RecordFallbackAsync(
                new WakeWordFallbackEvent(correlationId, WakeWordFallbackPhase.Published, state.ReasonCode, Exception: null),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
