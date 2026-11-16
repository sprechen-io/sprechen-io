namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Reads a right-sized PCM copy through the ring. The copy and the cursor advance belong to the history primitive.
/// </summary>
internal static class PcmRingChunkReader
{
    public static byte[]? ReadRightSizedChunk(
        SpeechSampleRing ring,
        ref SampleCursor cursor,
        int maxBytesPerCall)
    {
        ArgumentNullException.ThrowIfNull(ring);
        return ring.ReadRightSized(ref cursor, maxBytesPerCall);
    }
}
