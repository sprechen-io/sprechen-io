using Sprechen.Speech.ModelRuntime.PromptTemplates;
using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Versucht die Bereitschaft zur stillen Eingabe anzusagen und meldet den Abschluss mit der
/// Oberflächenrevision, damit nur der passende Übergang die Bearbeitung freigibt.
/// </summary>
internal sealed class SpeakSilentInputReadyEffect
    : IStateEffect<SpeechAssistantEvent, SpeechAssistantContext>
{
    private readonly long _expectedSurfaceRevision;
    private readonly string _isoCode;

    public SpeakSilentInputReadyEffect(long expectedSurfaceRevision, string isoCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        _expectedSurfaceRevision = expectedSurfaceRevision;
        _isoCode = isoCode;
    }

    public async ValueTask ExecuteAsync(
        SpeechAssistantContext context,
        IEventDispatcher<SpeechAssistantEvent> dispatcher,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(dispatcher);

        try
        {
            var text = FormDialogFlowPromptTextResolver.GetSilentInputReadyPrompt(_isoCode);
            await context.InteractionDependencies.TextToSpeechPlayer
                .SpeakAsync(text, _isoCode, cancellationToken)
                .ConfigureAwait(false);
            await dispatcher
                .DispatchAsync(
                    new SpeechAssistantSilentInputReadySpokenEvent(_expectedSurfaceRevision),
                    DispatchMode.Enqueue,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            await dispatcher
                .DispatchAsync(
                    new SpeechAssistantSilentInputReadyPlaybackFailedEvent(_expectedSurfaceRevision),
                    DispatchMode.Enqueue,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
