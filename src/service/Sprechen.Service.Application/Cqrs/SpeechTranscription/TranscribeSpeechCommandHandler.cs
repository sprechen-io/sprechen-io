using Microsoft.Extensions.Logging;
using Monstertacle.SharpQrs;

namespace Sprechen.Service.Application.Cqrs.SpeechTranscription;

/// <summary>
/// Fordert das abschließende Transkript an und kennzeichnet fehlende oder leere Ergebnisse.
/// </summary>
internal sealed class TranscribeSpeechCommandHandler
    : ICommandHandler<TranscribeSpeechCommand, TranscribeSpeechResult>
{
    private readonly ISpeechToTextService _speechToText;
    private readonly ILogger<TranscribeSpeechCommandHandler> _logger;

    public TranscribeSpeechCommandHandler(
        ISpeechToTextService speechToText,
        ILogger<TranscribeSpeechCommandHandler> logger)
    {
        _speechToText = speechToText ?? throw new ArgumentNullException(nameof(speechToText));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TranscribeSpeechResult> HandleAsync(
        TranscribeSpeechCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Chunks);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.LanguageCode);

        var transcript = await _speechToText
            .TranscribeStreamingAsync(command.Chunks, command.MimeType, cancellationToken, command.LanguageCode, command.RecognitionProfile, command.Keyterms, command.ClientCorrelationId, command.IsDiagnostic)
            .ConfigureAwait(false);

        if (transcript is null)
        {
            _logger.LogDebug(
                "TranscribeSpeech command received null transcript (languageCode={LanguageCode}, mimeType={MimeType}).",
                command.LanguageCode,
                command.MimeType);
            return new TranscribeSpeechResult(new SpeechTranscript(string.Empty, null), IsEmpty: true);
        }

        var isEmpty = string.IsNullOrWhiteSpace(transcript.Text);
        return new TranscribeSpeechResult(transcript, isEmpty);
    }
}
