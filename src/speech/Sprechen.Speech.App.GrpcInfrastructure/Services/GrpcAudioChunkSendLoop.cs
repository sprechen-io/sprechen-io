using Grpc.Core;
using Sprechen.Proto;
using AppRecognitionProfile = Sprechen.Speech.App.Contracts.Services.RecognitionProfile;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Sendet nichtleere Audioblöcke in ihrer Reihenfolge über gRPC und schließt den
/// Anfragestream. Bei Sendefehlern wird der Abschluss vor dem Weiterwerfen versucht.
/// </summary>
internal sealed class GrpcAudioChunkSendLoop
{
    public async Task WriteAsync(
        IReadOnlyList<byte[]> chunks,
        IClientStreamWriter<AudioChunk> requestStream,
        SpeechAudioMimeType mimeType,
        CancellationToken cancellationToken,
        string? languageCode = null,
        AppRecognitionProfile recognitionProfile = AppRecognitionProfile.Conversational,
        SpeechKeyterms? keyterms = null,
        bool isDiagnostic = false)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        await WriteCoreAsync(
                chunks,
                requestStream,
                new GrpcAudioChunkMessageFactory(mimeType, languageCode, recognitionProfile, keyterms, isDiagnostic),
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task WriteAsync(
        IAsyncEnumerable<byte[]> chunks,
        IClientStreamWriter<AudioChunk> requestStream,
        SpeechAudioMimeType mimeType,
        CancellationToken cancellationToken,
        string? languageCode = null,
        AppRecognitionProfile recognitionProfile = AppRecognitionProfile.Conversational,
        SpeechKeyterms? keyterms = null,
        bool isDiagnostic = false)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentNullException.ThrowIfNull(requestStream);

        var messageFactory = new GrpcAudioChunkMessageFactory(mimeType, languageCode, recognitionProfile, keyterms, isDiagnostic);
        try
        {
            await foreach (var chunk in chunks.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                await WriteChunkAsync(requestStream, messageFactory, chunk, cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            await CompleteBestEffortAsync(requestStream).ConfigureAwait(false);
            throw;
        }

        await requestStream.CompleteAsync().ConfigureAwait(false);
    }

    private static async Task WriteCoreAsync(
        IReadOnlyList<byte[]> chunks,
        IClientStreamWriter<AudioChunk> requestStream,
        GrpcAudioChunkMessageFactory messageFactory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestStream);

        try
        {
            foreach (var chunk in chunks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await WriteChunkAsync(requestStream, messageFactory, chunk, cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            await CompleteBestEffortAsync(requestStream).ConfigureAwait(false);
            throw;
        }

        await requestStream.CompleteAsync().ConfigureAwait(false);
    }

    private static async Task WriteChunkAsync(
        IClientStreamWriter<AudioChunk> requestStream,
        GrpcAudioChunkMessageFactory messageFactory,
        byte[]? chunk,
        CancellationToken cancellationToken)
    {
        if (chunk is null || chunk.Length == 0)
        {
            return;
        }

        await requestStream.WriteAsync(messageFactory.Create(chunk), cancellationToken).ConfigureAwait(false);
    }

    private static async Task CompleteBestEffortAsync(IClientStreamWriter<AudioChunk> requestStream)
    {
        try
        {
            await requestStream.CompleteAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Ein Fehler beim Schließen soll den ursprünglichen Sendefehler nicht ersetzen.
        }
    }
}
