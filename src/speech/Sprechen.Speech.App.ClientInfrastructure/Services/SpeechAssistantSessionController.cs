using System.Runtime.ExceptionServices;

namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Stoppt den Sprachassistenten und setzt anschließend den Aktivierungszustand
/// zurück. Einen zuvor erfassten Stoppfehler gibt er danach weiter.
/// </summary>
public sealed class SpeechAssistantSessionController : ISpeechAssistantSessionController
{
    private readonly ISpeechAssistantService _speechAssistant;
    private readonly ISpeechCaptureFeedback _speechCaptureFeedback;

    public SpeechAssistantSessionController(
        ISpeechAssistantService speechAssistant,
        ISpeechCaptureFeedback speechCaptureFeedback)
    {
        _speechAssistant = speechAssistant ?? throw new ArgumentNullException(nameof(speechAssistant));
        _speechCaptureFeedback = speechCaptureFeedback ?? throw new ArgumentNullException(nameof(speechCaptureFeedback));
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken)
    {
        ExceptionDispatchInfo? capturedFailure = null;

        try
        {
            await _speechAssistant.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            capturedFailure = ExceptionDispatchInfo.Capture(ex);
        }

        await _speechCaptureFeedback.SetActivationStateAsync(SpeechAssistantActivationState.NotStarted).ConfigureAwait(false);
        capturedFailure?.Throw();
    }
}
