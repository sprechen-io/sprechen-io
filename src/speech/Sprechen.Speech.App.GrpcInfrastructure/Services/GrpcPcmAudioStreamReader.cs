using Grpc.Core;
using Sprechen.Proto;
using System.Runtime.CompilerServices;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Fügt über gRPC-Blockgrenzen getrennte Bytes zu vollständigen 16-Bit-Samples zusammen,
/// damit die Wiedergabe keine halben Samples erhält. Nichtleere PCM-Antworten bestimmen
/// die zuletzt gelesene positive Abtastrate. Bis dahin gilt der Standardwert.
/// </summary>
internal sealed class GrpcPcmAudioStreamReader
{
    public async IAsyncEnumerable<(byte[] bytes, int sampleRateHz)> ReadAlignedPcm16LeAsync(
        IAsyncStreamReader<AudioResponse> responseStream,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(responseStream);

        var alignmentBuffer = new Pcm16LeAlignmentBuffer();
        var sampleRateHz = PcmAudioDefaults.DEFAULT_SAMPLE_RATE_HZ;

        while (await responseStream.MoveNext(cancellationToken).ConfigureAwait(false))
        {
            var current = responseStream.Current;
            if (current?.Data is null || current.Data.IsEmpty)
            {
                continue;
            }

            if (current.MimeType != AudioMimeType.AudioPcm)
            {
                throw new InvalidOperationException($"Expected PCM audio but got '{current.MimeType}'.");
            }

            if (current.SampleRateHz > 0)
            {
                sampleRateHz = current.SampleRateHz;
            }

            var bytes = alignmentBuffer.Align(current.Data.ToByteArray());

            if (bytes.Length > 0)
            {
                yield return (bytes, sampleRateHz);
            }
        }
    }
}

