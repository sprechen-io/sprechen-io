using Grpc.Core;
using Sprechen.Proto;
using Sprechen.Web.HostSupport.Grpc.Proxy;

namespace Sprechen.Web.HostSupport.Grpc.Adapter;

/// <summary>
/// Erfasst Ergebnis und Audiozähler eines Weiterleitungsversuchs auch bei definierten Abbruchausnahmen.
/// </summary>
internal sealed class SpeechProxyTextToSpeechForwardingAttempt
{
    private readonly SpeechProxyAudioStreamForwarder _audioStreamForwarder = new();

    public async Task<SpeechProxyTextToSpeechOutcome> TryForwardAsync(
        Func<CancellationToken, AsyncServerStreamingCall<AudioResponse>> startCall,
        IServerStreamWriter<AudioResponse> responseStream,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(startCall);
        ArgumentNullException.ThrowIfNull(responseStream);

        // Den Akkumulator außerhalb des Weiterleitungsaufrufs halten, damit seine Werte bei einem Abbruch noch verfügbar sind.
        var accumulator = new SpeechProxyAudioStreamAccumulator();

        try
        {
            using var call = startCall(cancellationToken);
            var summary = await _audioStreamForwarder.StreamAsync(call, responseStream, accumulator, cancellationToken)
                .ConfigureAwait(false);
            return new SpeechProxyTextToSpeechOutcome(
                Aborted: false, summary.ChunkCount, summary.ByteCount, summary.FirstSampleRateHz);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            return Abandoned(accumulator);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Abandoned(accumulator);
        }
        catch (InvalidOperationException) when (cancellationToken.IsCancellationRequested)
        {
            return Abandoned(accumulator);
        }
    }

    private static SpeechProxyTextToSpeechOutcome Abandoned(SpeechProxyAudioStreamAccumulator accumulator)
        => new(Aborted: true, accumulator.ChunkCount, accumulator.ByteCount, accumulator.FirstSampleRateHz);
}
