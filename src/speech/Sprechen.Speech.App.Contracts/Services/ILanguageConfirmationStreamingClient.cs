namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Öffnet eine Streaming-Sitzung zur Sprachbestätigung mit einer optional vorgegebenen Sprache.
/// </summary>
public interface ILanguageConfirmationStreamingClient
{
    Task<ILanguageConfirmationStreamingSession> StartAsync(string? languageCode, CancellationToken cancellationToken);
}


