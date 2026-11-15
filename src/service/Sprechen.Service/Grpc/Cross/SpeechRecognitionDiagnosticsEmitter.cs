using Microsoft.Extensions.Options;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Zählt leere Transkripte nach Operation, Anbieter, Sprache und Audioformat.
/// </summary>
internal sealed class SpeechRecognitionDiagnosticsEmitter(
    IOptions<SpeechProviderOptions> speechProviderOptions)
{
    private readonly IOptions<SpeechProviderOptions> _speechProviderOptions = speechProviderOptions ?? throw new ArgumentNullException(nameof(speechProviderOptions));

    public SpeechProvider SttProvider => _speechProviderOptions.Value.SttProvider;

    public void RecordEmptyTranscript(string operation, string languageCode, AudioMimeType mimeType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);

        SpeechRecognitionTelemetry.RecordEmptyTranscript(
            operation,
            SttProvider.ToString(),
            languageCode,
            mimeType.ToString());
    }

}
