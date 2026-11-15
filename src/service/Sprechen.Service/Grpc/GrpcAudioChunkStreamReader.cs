using Google.Protobuf;
using Grpc.Core;
using Sprechen.Service.Core.Services;
using System.Runtime.CompilerServices;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Grpc;

/// <summary>
/// Liest Metadaten und puffert Audio bis zum ersten Segment mit gesetztem MIME-Typ.
/// Danach werden aus dem verbleibenden Strom nur noch Audiodaten gelesen.
/// </summary>
internal sealed class GrpcAudioChunkStreamReader
{
    private readonly SpeechKeytermPolicy _keytermPolicy = new();

    public async Task<GrpcAudioChunkStreamStart> ReadStartAsync(
        IAsyncStreamReader<SprechenProto.AudioChunk> requestStream,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(requestStream);

        var mimeType = AudioMimeType.Unspecified;
        var providedMimeType = SprechenProto.AudioMimeType.AudioMimeUnspecified;
        string? languageCode = null;
        var recognitionProfile = SprechenProto.RecognitionProfile.Unspecified;
        SpeechKeyterms? keyterms = null;
        // Zusammensetzung getrennt lesen: WithoutServerBase bleibt auch mit leerer Begriffsliste wirksam.
        var composition = SprechenProto.KeytermComposition.Unspecified;
        var diagnosticSession = false;
        var chunks = new List<byte[]>();

        while (!ct.IsCancellationRequested && await requestStream.MoveNext(ct).ConfigureAwait(false))
        {
            var chunk = requestStream.Current;
            if (languageCode is null && !string.IsNullOrWhiteSpace(chunk.LanguageCode))
            {
                languageCode = chunk.LanguageCode.Trim();
            }

            // Ein gesetztes Diagnoseflag bleibt innerhalb des eingelesenen Startabschnitts erhalten.
            if (!diagnosticSession && chunk.DiagnosticSession)
            {
                diagnosticSession = true;
            }

            if (recognitionProfile == SprechenProto.RecognitionProfile.Unspecified &&
                chunk.RecognitionProfile != SprechenProto.RecognitionProfile.Unspecified)
            {
                recognitionProfile = chunk.RecognitionProfile;
            }

            if (composition == SprechenProto.KeytermComposition.Unspecified &&
                chunk.KeytermComposition != SprechenProto.KeytermComposition.Unspecified)
            {
                composition = chunk.KeytermComposition;
            }

            if (keyterms is null &&
                (chunk.Keyterms.Count > 0 ||
                 composition == SprechenProto.KeytermComposition.WithoutServerBase))
            {
                try
                {
                    // Zusammensetzung und Begriffe gemeinsam normalisieren, damit das passende Größenlimit gilt.
                    keyterms = _keytermPolicy.Normalize(
                        new SpeechKeyterms(
                            chunk.Keyterms.ToArray(),
                            GrpcKeytermCompositionMapper.ToCore(composition)));
                }
                catch (ArgumentException ex)
                {
                    throw SprechenRpcExceptionFactory.InvalidArgument(
                        SprechenProto.SprechenErrorCode.SpeechKeytermsInvalid,
                        ex.Message);
                }
            }

            if (providedMimeType == SprechenProto.AudioMimeType.AudioMimeUnspecified &&
                chunk.MimeType != SprechenProto.AudioMimeType.AudioMimeUnspecified)
            {
                providedMimeType = chunk.MimeType;
                mimeType = GrpcAudioMimeTypeMapper.ToCore(providedMimeType);
            }

            if (chunk.Data is not null && !chunk.Data.IsEmpty)
            {
                chunks.Add(chunk.Data.ToByteArray());
            }

            if (providedMimeType != SprechenProto.AudioMimeType.AudioMimeUnspecified)
            {
                break;
            }
        }

        return new GrpcAudioChunkStreamStart(
            mimeType,
            providedMimeType,
            languageCode,
            chunks,
            GrpcRecognitionProfileMapper.ToCore(recognitionProfile),
            keyterms,
            diagnosticSession);
    }

    public async IAsyncEnumerable<byte[]> ReadAllChunksAsync(
        IReadOnlyList<byte[]> bufferedChunks,
        IAsyncStreamReader<SprechenProto.AudioChunk> requestStream,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(bufferedChunks);
        ArgumentNullException.ThrowIfNull(requestStream);

        foreach (var chunk in bufferedChunks)
        {
            if (chunk.Length == 0)
            {
                continue;
            }

            yield return chunk;
        }

        while (await requestStream.MoveNext(ct).ConfigureAwait(false))
        {
            var chunk = requestStream.Current;
            if (chunk.Data is null || chunk.Data.IsEmpty)
            {
                continue;
            }

            yield return chunk.Data.ToByteArray();
        }
    }
}
