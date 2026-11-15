namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Wertet eine vorliegende Blockfolge bis zur ersten fertigen Äußerung aus,
/// damit deren Blockanzahl mit derselben Segmentierungslogik wie bei laufender Aufnahme bestimmbar ist.
/// </summary>
public static class PcmSpeechCaptureSegmentation
{
    public static int SegmentChunkSequence(
        IReadOnlyList<byte[]> chunks,
        int silenceDebounceMs,
        PcmSpeechCaptureSegmenterOptions options)
    {
        return SegmentChunkSequence(chunks, silenceDebounceMs, SpeechAudioMimeType.Pcm, options);
    }

    public static int SegmentChunkSequence(
        IReadOnlyList<byte[]> chunks,
        int silenceDebounceMs,
        SpeechAudioMimeType mimeType,
        PcmSpeechCaptureSegmenterOptions options)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentNullException.ThrowIfNull(options);

        var segmenter = new PcmSpeechCaptureSegmenter(mimeType, silenceDebounceMs, options);
        CapturedSpeechAudio? utterance = null;

        foreach (var chunk in chunks)
        {
            var update = segmenter.ProcessChunk(chunk);
            if (update.CompletionException is not null)
            {
                throw update.CompletionException;
            }

            if (update.CompletedUtterance is not null)
            {
                utterance = update.CompletedUtterance;
                break;
            }
        }

        return utterance?.Chunks.Count ?? 0;
    }
}


