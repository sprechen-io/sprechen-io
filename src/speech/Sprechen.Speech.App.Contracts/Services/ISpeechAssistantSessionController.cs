namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Stellt das Beenden der Assistentensitzung bereit.
/// </summary>
public interface ISpeechAssistantSessionController
{
    ValueTask StopAsync(CancellationToken cancellationToken);
}
