using Microsoft.Extensions.Logging;
using Sprechen.Speech.State;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Versucht, den Bereitschaftston vor der Erkennung des Aktivierungsworts abzuspielen.
/// Wiedergabefehler werden protokolliert, damit sie die anschließende Erkennung nicht verhindern.
/// </summary>
internal sealed class PlayArmedListeningCueEffect
    : IStateEffect<SpeechAssistantEvent, SpeechAssistantContext>
{
    public async ValueTask ExecuteAsync(
        SpeechAssistantContext context,
        IEventDispatcher<SpeechAssistantEvent> dispatcher,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(dispatcher);

        try
        {
            await context.InteractionDependencies.EarconPlayer
                .PlayAsync(EarconId.FlowListening, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            context.InteractionDependencies.Logger.LogDebug(ex, "Armed listening cue playback failed.");
        }
    }
}
