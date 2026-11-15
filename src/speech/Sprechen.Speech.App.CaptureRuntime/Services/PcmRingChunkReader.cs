using System.Buffers;

namespace Sprechen.Speech.App.CaptureRuntime.Services;

/// <summary>
/// Liest verfügbare Bytes aus <see cref="SpeechSampleRing"/> in einen wiederverwendbaren
/// Puffer von <see cref="ArrayPool{T}"/> und verschiebt den Cursor um die gelesene Bytezahl.
/// Das Ergebnis ist eine eigene, passend große Kopie, damit der Zwischenpuffer sofort
/// zurückgegeben werden kann. Ohne verfügbare Bytes wird <c>null</c> zurückgegeben.
/// </summary>
internal static class PcmRingChunkReader
{
    public static byte[]? ReadRightSizedChunk(
        SpeechSampleRing ring,
        ref SampleCursor cursor,
        int maxBytesPerCall)
    {
        ArgumentNullException.ThrowIfNull(ring);

        // Rent kann ein größeres Array liefern. Trotzdem höchstens
        // maxBytesPerCall Bytes lesen, damit die Aufrufgrenze erhalten bleibt.
        var scratch = ArrayPool<byte>.Shared.Rent(maxBytesPerCall);
        try
        {
            var bytesRead = ring.Read(cursor, scratch.AsSpan(0, maxBytesPerCall));
            if (bytesRead == 0)
            {
                return null;
            }

            var chunk = scratch.AsSpan(0, bytesRead).ToArray();
            cursor = cursor with { SampleIndex = cursor.SampleIndex + bytesRead };
            return chunk;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(scratch);
        }
    }
}
