using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Übersetzt Aufrufe der stillen Eingabesteuerung in Ereignisse für den gestarteten Dialoghost.
/// Identitätsprüfung und Zustandsentscheidung bleiben beim Reducer.
/// </summary>
internal sealed class SpeechAssistantSilentInputActions
{
    private readonly SpeechAssistantLifecycle _lifecycle;

    public SpeechAssistantSilentInputActions(SpeechAssistantLifecycle lifecycle)
    {
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
    }

    public ValueTask EnterAsync(
        SpeechAssistantSilentInputSnapshotIdentity expected,
        CancellationToken cancellationToken)
        => DispatchAsync(new SpeechAssistantSilentInputEnterRequestedEvent(expected), cancellationToken);

    public ValueTask SubmitAsync(
        SpeechAssistantSilentInputSnapshotIdentity expected,
        string value,
        CancellationToken cancellationToken)
        => DispatchAsync(new SpeechAssistantSilentInputSubmitRequestedEvent(expected, value), cancellationToken);

    public ValueTask ClearAsync(
        SpeechAssistantSilentInputSnapshotIdentity expected,
        CancellationToken cancellationToken)
        => DispatchAsync(new SpeechAssistantSilentInputClearRequestedEvent(expected), cancellationToken);

    public ValueTask CancelAsync(
        SpeechAssistantSilentInputSnapshotIdentity expected,
        CancellationToken cancellationToken)
        => DispatchAsync(new SpeechAssistantSilentInputCancelRequestedEvent(expected), cancellationToken);

    private async ValueTask DispatchAsync(SpeechAssistantEvent @event, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);
        var host = _lifecycle.GetStartedHostOrThrow(nameof(ISpeechAssistantSilentInputController));
        await host.DispatchToActiveAsync(@event, DispatchMode.Enqueue, cancellationToken).ConfigureAwait(false);
    }
}
