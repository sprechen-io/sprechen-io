using Google.Protobuf;
using Grpc.Core;
using Sprechen.Proto.Transport;
using Sprechen.Service.Abstractions;
using Sprechen.Service.Grpc;
using Xunit;
using SprechenProto = Sprechen.Proto;

namespace Sprechen.Service.Tests.Grpc;

public sealed class GrpcAudioChunkStreamReaderKeytermTests
{
    [Fact]
    public async Task ReadStartAsync_AdmitsTheLargestRequestTheClientsProjectionWillEmit()
    {
        // Die Zusammensetzung muss das zulässige Begriffsbudget auch beim Einlesen bestimmen.
        var chunk = CreateStartChunk();
        chunk.KeytermComposition = SprechenProto.KeytermComposition.WithoutServerBase;
        chunk.Keyterms.AddRange(Enumerable.Range(0, SpeechKeytermPolicy.MaximumCountWithoutServerBase)
            .Select(index => $"term{index}"));
        var stream = new SequenceAsyncStreamReader<SprechenProto.AudioChunk>([chunk]);

        var start = await new GrpcAudioChunkStreamReader().ReadStartAsync(stream, CancellationToken.None);

        Assert.NotNull(start.Keyterms);
        Assert.Equal(KeytermComposition.WithoutServerBase, start.Keyterms!.Composition);
        Assert.True(
            start.Keyterms.Terms.Count > SpeechKeytermPolicy.MaximumCount,
            $"A without-server-base turn may carry up to {SpeechKeytermPolicy.MaximumCountWithoutServerBase} "
            + $"terms; the reader admitted only {start.Keyterms.Terms.Count}, which is the with-server-base "
            + "budget applied to a turn that asked for its terms alone.");
    }

    [Fact]
    public async Task ReadStartAsync_WithServerBase_StillHoldsTheReservedBudget()
    {
        // Mit Servervokabular bleibt das kleinere Budget für Clientbegriffe bestehen.
        var chunk = CreateStartChunk();
        chunk.Keyterms.AddRange(Enumerable.Range(0, SpeechKeytermPolicy.MaximumCount + 5)
            .Select(index => $"term{index}"));
        var stream = new SequenceAsyncStreamReader<SprechenProto.AudioChunk>([chunk]);

        await Assert.ThrowsAsync<RpcException>(
            () => new GrpcAudioChunkStreamReader().ReadStartAsync(stream, CancellationToken.None));
    }

    [Fact]
    public async Task ReadStartAsync_WithoutServerBaseAndNoTerms_IsNotTheSameAsSayingNothing()
    {
        var chunk = CreateStartChunk();
        chunk.KeytermComposition = SprechenProto.KeytermComposition.WithoutServerBase;
        var stream = new SequenceAsyncStreamReader<SprechenProto.AudioChunk>([chunk]);

        var start = await new GrpcAudioChunkStreamReader().ReadStartAsync(stream, CancellationToken.None);

        // Eine leere Liste ohne Servervokabular bleibt von fehlenden Erkennungshinweisen unterscheidbar.
        Assert.NotNull(start.Keyterms);
        Assert.Empty(start.Keyterms!.Terms);
        Assert.Equal(KeytermComposition.WithoutServerBase, start.Keyterms.Composition);
    }

    [Fact]
    public async Task ReadStartAsync_NoKeytermsAndNoComposition_LeavesTheRequestUnbiased()
    {
        var stream = new SequenceAsyncStreamReader<SprechenProto.AudioChunk>([CreateStartChunk()]);

        var start = await new GrpcAudioChunkStreamReader().ReadStartAsync(stream, CancellationToken.None);

        Assert.Null(start.Keyterms);
    }

    [Fact]
    public async Task ReadStartAsync_ValidKeyterms_ReturnsTrimmedBoundedSnapshot()
    {
        var chunk = CreateStartChunk();
        chunk.Keyterms.Add("  Einfaches Beispiel  ");
        chunk.Keyterms.Add("Komplexes Beispiel");
        var stream = new SequenceAsyncStreamReader<SprechenProto.AudioChunk>([chunk]);

        var start = await new GrpcAudioChunkStreamReader().ReadStartAsync(stream, CancellationToken.None);

        Assert.NotNull(start.Keyterms);
        Assert.Equal(["Einfaches Beispiel", "Komplexes Beispiel"], start.Keyterms!.Terms);
        // Ohne explizite Zusammensetzung gilt WithServerBase.
        Assert.Equal(KeytermComposition.WithServerBase, start.Keyterms.Composition);
    }

    [Fact]
    public async Task ReadStartAsync_TooManyKeyterms_ReturnsCodedInvalidArgument()
    {
        var chunk = CreateStartChunk();
        chunk.Keyterms.Add(Enumerable.Range(0, SpeechKeytermPolicy.MaximumCount + 1)
            .Select(index => $"term-{index}"));

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            new GrpcAudioChunkStreamReader().ReadStartAsync(
                new SequenceAsyncStreamReader<SprechenProto.AudioChunk>([chunk]),
                CancellationToken.None));

        AssertInvalidKeyterms(exception);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("line\nbreak")]
    public async Task ReadStartAsync_InvalidKeytermContent_ReturnsCodedInvalidArgument(string keyterm)
    {
        var chunk = CreateStartChunk();
        chunk.Keyterms.Add(keyterm);

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            new GrpcAudioChunkStreamReader().ReadStartAsync(
                new SequenceAsyncStreamReader<SprechenProto.AudioChunk>([chunk]),
                CancellationToken.None));

        AssertInvalidKeyterms(exception);
    }

    [Fact]
    public async Task ReadStartAsync_OversizedKeyterm_ReturnsCodedInvalidArgument()
    {
        var chunk = CreateStartChunk();
        chunk.Keyterms.Add(new string('x', SpeechKeytermPolicy.MaximumTermLength + 1));

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            new GrpcAudioChunkStreamReader().ReadStartAsync(
                new SequenceAsyncStreamReader<SprechenProto.AudioChunk>([chunk]),
                CancellationToken.None));

        AssertInvalidKeyterms(exception);
    }

    [Fact]
    public async Task ReadStartAsync_ExcessiveAggregateKeytermBytes_ReturnsCodedInvalidArgument()
    {
        var chunk = CreateStartChunk();
        chunk.Keyterms.Add(Enumerable.Range(0, 5).Select(index => $"{index}-{new string('x', 118)}"));

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            new GrpcAudioChunkStreamReader().ReadStartAsync(
                new SequenceAsyncStreamReader<SprechenProto.AudioChunk>([chunk]),
                CancellationToken.None));

        AssertInvalidKeyterms(exception);
    }

    private static SprechenProto.AudioChunk CreateStartChunk()
        => new()
        {
            Data = ByteString.CopyFrom([0x1]),
            MimeType = SprechenProto.AudioMimeType.AudioPcm,
            LanguageCode = "de"
        };

    private static void AssertInvalidKeyterms(RpcException exception)
    {
        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
        Assert.True(SprechenGrpcErrorMetadata.TryGetErrorCode(exception, out var errorCode));
        Assert.Equal(SprechenProto.SprechenErrorCode.SpeechKeytermsInvalid, errorCode);
    }
}
