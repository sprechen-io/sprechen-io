using Bunit;
using Sprechen.Proto;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class GrpcAudioChunkOwnerTests : TestContext
{
    [Fact]
    public void Resolve_WhenMimeTypeMissing_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new SpeechAudioMimeTypeResolver().Resolve(" "));
        Assert.Contains("missing", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolve_WhenMimeTypeNotPcm_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new SpeechAudioMimeTypeResolver().Resolve("audio/mpeg"));
        Assert.Contains("Unsupported", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resolve_WhenMimeTypeContainsPcm_ReturnsPcm()
    {
        var actual = new SpeechAudioMimeTypeResolver().Resolve(" audio/pcm;rate=16000 ");

        Assert.Equal(SpeechAudioMimeType.Pcm, actual);
    }

    [Fact]
    public async Task WriteAsync_FromMicrophoneChunks_WhenLanguageCodeProvided_SendsLanguageCodeOnlyOnFirstChunk()
    {
        await using var recording = ScriptedMicrophoneRecording.Create(static (_, _) => Task.CompletedTask);
        recording.SetMimeType("audio/pcm;rate=16000");
        var writer = new CollectingClientStreamWriter<AudioChunk>();

        var writeTask = new GrpcAudioChunkSendLoop().WriteAsync(
            recording.Chunks,
            writer,
            SpeechAudioMimeType.Pcm,
            CancellationToken.None,
            languageCode: "de");

        recording.OnChunk(new byte[] { 1, 2, 3 });
        recording.OnChunk(new byte[] { 4, 5, 6 });
        recording.OnStopped();

        await writeTask;

        Assert.True(writer.Completed);
        Assert.Equal(2, writer.Messages.Count);

        var first = writer.Messages[0];
        Assert.Equal(AudioMimeType.AudioPcm, first.MimeType);
        Assert.Equal("de", first.LanguageCode);
        Assert.Equal(new byte[] { 1, 2, 3 }, first.Data.ToByteArray());

        var second = writer.Messages[1];
        Assert.Equal(AudioMimeType.AudioPcm, second.MimeType);
        Assert.True(string.IsNullOrWhiteSpace(second.LanguageCode));
        Assert.Equal(new byte[] { 4, 5, 6 }, second.Data.ToByteArray());
    }
}
