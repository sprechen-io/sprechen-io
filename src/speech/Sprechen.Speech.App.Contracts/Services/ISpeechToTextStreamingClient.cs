namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Öffnet eine Streaming-Sitzung zur laufenden Spracherkennung mit optionaler Sprachvorgabe.
/// </summary>
public interface ISpeechToTextStreamingClient
{
    Task<ISpeechToTextStreamingSession> StartAsync(string? languageCode, CancellationToken cancellationToken);
}


