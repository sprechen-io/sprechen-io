using Google.Protobuf;
using Sprechen.Service.Grpc;
using Xunit;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Tests.Grpc;

public sealed class GrpcConversationAudioInputStreamTests
{
    [Fact]
    public async Task ReadAllChunksAsync_WhenClientStopsSendingChunks_CompletesByInactivityTimeout()
    {
        var requestStream = new ChannelAsyncStreamReader<SprechenProto.AudioChunk>();
        var inputStream = new GrpcConversationAudioInputStream(TimeSpan.FromMilliseconds(30));
        var chunks = new List<byte[]>();

        await foreach (var chunk in inputStream.ReadAllChunksAsync(
                           new[] { new byte[] { 0x1, 0x2 } },
                           requestStream,
                           CancellationToken.None))
        {
            chunks.Add(chunk);
        }

        var bufferedChunk = Assert.Single(chunks);
        Assert.Equal(new byte[] { 0x1, 0x2 }, bufferedChunk);
        Assert.True(inputStream.CompletedByInactivityTimeout);
        Assert.Equal(GrpcConversationAudioInputCompletionReason.InactivityTimeout, inputStream.CompletionReason);
    }

    [Fact]
    public async Task ReadAllChunksAsync_WhenOnlyEmptyChunksArrive_DoesNotRefreshInactivityTimeout()
    {
        var requestStream = new ChannelAsyncStreamReader<SprechenProto.AudioChunk>();
        requestStream.Write(new SprechenProto.AudioChunk
        {
            Data = ByteString.Empty
        });

        var inputStream = new GrpcConversationAudioInputStream(TimeSpan.FromMilliseconds(30));
        var chunks = new List<byte[]>();

        await foreach (var chunk in inputStream.ReadAllChunksAsync(
                           Array.Empty<byte[]>(),
                           requestStream,
                           CancellationToken.None))
        {
            chunks.Add(chunk);
        }

        Assert.Empty(chunks);
        Assert.True(inputStream.CompletedByInactivityTimeout);
        Assert.Equal(GrpcConversationAudioInputCompletionReason.InactivityTimeout, inputStream.CompletionReason);
    }

    [Fact]
    public async Task ReadAllChunksAsync_WhenCancellationIsRequested_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        var requestStream = new ChannelAsyncStreamReader<SprechenProto.AudioChunk>();
        var inputStream = new GrpcConversationAudioInputStream(TimeSpan.FromSeconds(1));

        cts.CancelAfter(TimeSpan.FromMilliseconds(30));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in inputStream.ReadAllChunksAsync(
                               Array.Empty<byte[]>(),
                               requestStream,
                               cts.Token))
            {
            }
        });

        Assert.False(inputStream.CompletedByInactivityTimeout);
        Assert.Equal(GrpcConversationAudioInputCompletionReason.None, inputStream.CompletionReason);
    }

    [Fact]
    public async Task ReadAllChunksAsync_WhenRequestCompletes_NaturalCompletionIsRecorded()
    {
        var requestStream = new ChannelAsyncStreamReader<SprechenProto.AudioChunk>();
        requestStream.Complete();
        var inputStream = new GrpcConversationAudioInputStream(TimeSpan.FromSeconds(1));
        var chunks = new List<byte[]>();

        await foreach (var chunk in inputStream.ReadAllChunksAsync(
                           new[] { new byte[] { 0x1 } },
                           requestStream,
                           CancellationToken.None))
        {
            chunks.Add(chunk);
        }

        Assert.Single(chunks);
        Assert.False(inputStream.CompletedByInactivityTimeout);
        Assert.Equal(GrpcConversationAudioInputCompletionReason.Natural, inputStream.CompletionReason);
    }
}
