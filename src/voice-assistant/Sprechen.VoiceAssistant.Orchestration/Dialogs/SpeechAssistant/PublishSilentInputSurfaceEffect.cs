using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Überträgt den verfügbaren Formular- und Eingabezustand an die Oberflächensteuerung, damit diese
/// eine zum Reducer-Stand gehörende Momentaufnahme veröffentlichen kann.
/// </summary>
internal sealed class PublishSilentInputSurfaceEffect
    : IStateEffect<SpeechAssistantEvent, SpeechAssistantContext>
{
    private readonly SpeechAssistantState _state;

    public PublishSilentInputSurfaceEffect(SpeechAssistantState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public ValueTask ExecuteAsync(
        SpeechAssistantContext context,
        IEventDispatcher<SpeechAssistantEvent> dispatcher,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(dispatcher);

        var current = context.DialogDependencies.FormSurfaceState.Current;
        if (current is null || _state.SilentInput is null)
        {
            return ValueTask.CompletedTask;
        }

        var identity = string.IsNullOrWhiteSpace(_state.SilentInput.FieldId)
            ? null
            : new SpeechAssistantSilentInputSnapshotIdentity(
                _state.CorrelationId ?? string.Empty,
                _state.SessionId ?? string.Empty,
                _state.SurfaceRevision,
                _state.SelectedApplicationId ?? string.Empty,
                _state.SelectedFormId ?? string.Empty,
                _state.SilentInput.FieldId);

        var session = context.DialogDependencies.FormSurfaceController.BeginSession();
        _ = context.DialogDependencies.FormSurfaceController.TryPublishActive(
            session,
            new SpeechAssistantFormSurfaceView(current.Snapshot, _state.SilentInput, identity));
        return ValueTask.CompletedTask;
    }
}
