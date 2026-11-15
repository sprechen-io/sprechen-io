namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Hält ein unvollständiges PCM-Sample als einzelnes Byte zurück und verbindet
/// es mit dem nächsten nichtleeren Block zu vollständigen 16-Bit-Samples.
/// </summary>
internal sealed class Pcm16LeAlignmentBuffer
{
    private byte? _carry;

    public byte[] Align(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        if (bytes.Length == 0)
        {
            return [];
        }

        var merged = MergeCarry(bytes);
        if ((merged.Length & 1) == 0)
        {
            return merged;
        }

        _carry = merged[^1];
        return merged.Length == 1 ? [] : merged[..^1];
    }

    private byte[] MergeCarry(byte[] bytes)
    {
        if (_carry is null)
        {
            return bytes;
        }

        var merged = new byte[bytes.Length + 1];
        merged[0] = _carry.Value;
        bytes.CopyTo(merged.AsSpan(1));
        _carry = null;
        return merged;
    }
}
