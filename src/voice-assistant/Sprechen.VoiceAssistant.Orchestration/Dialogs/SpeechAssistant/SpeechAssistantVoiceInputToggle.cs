using Microsoft.Extensions.Logging;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Koordiniert den manuellen Wechsel zwischen Zuhören und Pause mit Hostereignissen, Zustandskopie
/// und Mikrofonsignalen, damit diese Schritte außerhalb des Reducers ausgeführt werden.
/// </summary>
internal sealed class SpeechAssistantVoiceInputToggle
{
    private readonly SpeechAssistantContext _context;
    private readonly SpeechAssistantLifecycle _lifecycle;
    private readonly Func<SpeechAssistantState> _readState;
    private readonly Action<SpeechAssistantState> _writeState;

    public SpeechAssistantVoiceInputToggle(
        SpeechAssistantContext context,
        SpeechAssistantLifecycle lifecycle,
        Func<SpeechAssistantState> readState,
        Action<SpeechAssistantState> writeState)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        _readState = readState ?? throw new ArgumentNullException(nameof(readState));
        _writeState = writeState ?? throw new ArgumentNullException(nameof(writeState));
    }

    public async ValueTask<SpeechAssistantActivationState> ToggleAsync(CancellationToken cancellationToken)
    {
        var host = _lifecycle.GetStartedHostOrThrow(nameof(SpeechAssistantService.ToggleVoiceInputAsync));
        var state = _readState();
        if (state.Phase == SpeechAssistantPhase.Paused)
        {
            await PlayToggleEarconAsync(EarconId.VoiceInputMicOn, cancellationToken).ConfigureAwait(false);
            await host.DispatchToActiveAsync(new SpeechAssistantVoiceInputResumedEvent(), DispatchMode.Enqueue, cancellationToken)
                .ConfigureAwait(false);
            RestorePausedStateCache(state);
            return SpeechAssistantActivationState.Ready;
        }

        if (state.Phase is SpeechAssistantPhase.Armed or SpeechAssistantPhase.Engaged)
        {
            var pause = new SpeechAssistantVoiceInputPausedEvent();
            // Reserve the pause ahead of any partial result published by an interrupted dialog.
            // The reducer treats a second pause in the paused phase as a no-op.
            await host.DispatchToActiveAsync(pause, DispatchMode.Enqueue, cancellationToken)
                .ConfigureAwait(false);
            await host.DispatchToActiveAsync(
                pause, DispatchMode.Interrupt, SpeechAssistantTerminalCancellation.ForRequiredTerminalWork())
                .ConfigureAwait(false);
            _writeState(
                state with
                {
                    Phase = SpeechAssistantPhase.Paused,
                    PauseSnapshot = SpeechAssistantPauseSnapshot.Capture(state)
                });
            await _context.InteractionDependencies.SpeechCaptureClient
                .PauseAsync(SpeechAssistantTerminalCancellation.ForRequiredTerminalWork())
                .ConfigureAwait(false);
            await PlayToggleEarconAsync(EarconId.VoiceInputMicOff, cancellationToken).ConfigureAwait(false);
            return SpeechAssistantActivationState.Paused;
        }

        if (state.Phase == SpeechAssistantPhase.Idle)
        {
            await host.DispatchToActiveAsync(new SpeechAssistantPressedEvent(), DispatchMode.Enqueue, cancellationToken)
                .ConfigureAwait(false);
        }

        return SpeechAssistantActivationState.Ready;
    }

    private void RestorePausedStateCache(SpeechAssistantState state)
    {
        var snapshot = state.PauseSnapshot
            ?? throw new InvalidOperationException("Paused speech assistant state is missing its resume snapshot.");

        _writeState(
            state with
            {
                Phase = snapshot.Phase,
                ActiveDialog = snapshot.ActiveDialog,
                CorrelationId = snapshot.CorrelationId,
                SelectedIsoCode = snapshot.SelectedIsoCode,
                SelectedApplicationId = snapshot.SelectedApplicationId,
                SelectedFormId = snapshot.SelectedFormId,
                PauseSnapshot = null
            });
    }

    private async ValueTask PlayToggleEarconAsync(EarconId earcon, CancellationToken cancellationToken)
    {
        try
        {
            await _context.InteractionDependencies.EarconPlayer.PlayAsync(earcon, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _context.InteractionDependencies.Logger.LogDebug(ex, "Voice input toggle earcon playback failed: {Earcon}.", earcon);
        }
    }
}
