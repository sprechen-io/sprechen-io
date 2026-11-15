namespace Sprechen.Service.Abstractions;

/// <summary>
/// Liest einen Audiostrom oder eine Folge von Audioblöcken vollständig in ein Bytearray.
/// </summary>
public static class AudioPayloadReader
{
    public static async Task<byte[]> ReadAllAsync(Stream stream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        await using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
        return memoryStream.ToArray();
    }

    public static async Task<byte[]> ReadAllAsync(IAsyncEnumerable<byte[]> chunks, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        await using var memoryStream = new MemoryStream();
        await foreach (var chunk in chunks.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (chunk.Length == 0)
            {
                continue;
            }

            await memoryStream.WriteAsync(chunk, cancellationToken).ConfigureAwait(false);
        }

        return memoryStream.ToArray();
    }
}
