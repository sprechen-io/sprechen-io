namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Bündelt die Bedienaktionen zum Starten, Pausieren und Stoppen des Sprachassistenten.
/// </summary>
public interface ISpeechAssistantService
{
    ValueTask<SpeechAssistantStartOutcome> StartAsync(CancellationToken cancellationToken);

    ValueTask<SpeechAssistantActivationState> ToggleVoiceInputAsync(CancellationToken cancellationToken);

    ValueTask HandleSpeechCapturePressAsync(CancellationToken cancellationToken);

    ValueTask StopAsync(CancellationToken cancellationToken);
}



