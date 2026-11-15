using Grpc.Core;
using Sprechen.Proto;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Bereitet den PCM-Player vor, reiht ausgerichtete gRPC-Audiodaten ein und
/// hält den Einreihestatus für die Wiederholungsentscheidung nach.
/// </summary>
internal sealed class TextToSpeechPcmStreamPlayer
{
    private readonly PcmAudioPlaybackLifecycle _pcmPlayback;
    private readonly TtsOutputLoudnessMakeup _makeupCatalog;

    public TextToSpeechPcmStreamPlayer(PcmAudioPlaybackLifecycle pcmPlayback, TtsOutputLoudnessMakeup makeupCatalog)
    {
        _pcmPlayback = pcmPlayback ?? throw new ArgumentNullException(nameof(pcmPlayback));
        _makeupCatalog = makeupCatalog ?? throw new ArgumentNullException(nameof(makeupCatalog));
    }

    public async Task PrepareAsync(string languageCode, CancellationToken cancellationToken)
    {
        await _pcmPlayback.EnsureInitializedAsync(PcmAudioDefaults.DEFAULT_SAMPLE_RATE_HZ, cancellationToken);
        await _pcmPlayback.ResetAsync(cancellationToken);
        await _pcmPlayback.SetMakeupAsync(_makeupCatalog.GetFactor(languageCode), cancellationToken);
    }

    public async Task<int> PlayAsync(
        AsyncServerStreamingCall<AudioResponse> call,
        TextToSpeechPcmEnqueueProgressState enqueueProgress,
        Action onFirstPcmEnqueued,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(call);
        ArgumentNullException.ThrowIfNull(enqueueProgress);
        ArgumentNullException.ThrowIfNull(onFirstPcmEnqueued);

        var reader = new GrpcPcmAudioStreamReader();
        var chunkCount = 0;
        await foreach (var (bytes, sampleRateHz) in reader.ReadAlignedPcm16LeAsync(call.ResponseStream, cancellationToken))
        {
            await _pcmPlayback.EnsureInitializedAsync(sampleRateHz, cancellationToken);
            enqueueProgress.MarkEnqueueStarted();
            await _pcmPlayback.EnqueuePcm16LeAsync(bytes, cancellationToken);
            enqueueProgress.MarkEnqueued();
            if (chunkCount == 0)
            {
                onFirstPcmEnqueued();
            }

            chunkCount++;
        }

        return chunkCount;
    }

    public ValueTask DrainAsync(CancellationToken cancellationToken) => _pcmPlayback.DrainAsync(cancellationToken);

    public Task ResetBestEffortAsync() => _pcmPlayback.ResetBestEffortAsync();
}
