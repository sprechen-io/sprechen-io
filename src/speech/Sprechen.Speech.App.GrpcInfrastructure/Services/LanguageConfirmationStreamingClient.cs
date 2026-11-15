using Sprechen.Proto;
using SpeechGrpcClient = Sprechen.Proto.Speech.SpeechClient;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Startet nach Vorbereitung der Spracheingabe eine gRPC-Sitzung mit lokaler Audioquelle
/// und Sprachkatalog, damit das Bestätigungsergebnis die verfügbaren lesbaren Sprachbezeichnungen erhält.
/// </summary>
internal sealed class LanguageConfirmationStreamingClient(
    ISpeechCaptureLocalSessionChunkSourceFactory localSessionChunkSourceFactory,
    IUserSpeechBargeInHandler bargeInHandler,
    ISupportedLanguagesClient supportedLanguagesClient,
    SpeechGrpcClient languageClient) : ILanguageConfirmationStreamingClient
{
    private readonly ISpeechCaptureLocalSessionChunkSourceFactory _localSessionChunkSourceFactory =
        localSessionChunkSourceFactory ?? throw new ArgumentNullException(nameof(localSessionChunkSourceFactory));
    private readonly IUserSpeechBargeInHandler _bargeInHandler = bargeInHandler ?? throw new ArgumentNullException(nameof(bargeInHandler));
    private readonly ISupportedLanguagesClient _supportedLanguagesClient = supportedLanguagesClient ?? throw new ArgumentNullException(nameof(supportedLanguagesClient));
    private readonly SpeechGrpcClient _languageClient = languageClient ?? throw new ArgumentNullException(nameof(languageClient));

    public async Task<ILanguageConfirmationStreamingSession> StartAsync(string? languageCode, CancellationToken cancellationToken)
    {
        await _bargeInHandler.OnSpeechCaptureStartingAsync(cancellationToken);

        var supportedLanguages = await _supportedLanguagesClient.GetAsync(cancellationToken).ConfigureAwait(false);
        var audioSource = await _localSessionChunkSourceFactory.CreateAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var speechCall = _languageClient.SpeechToConversationStream(cancellationToken: cancellationToken);

            return new LanguageConfirmationStreamingSession(audioSource, speechCall, languageCode, supportedLanguages, cancellationToken);
        }
        catch
        {
            await audioSource.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}


