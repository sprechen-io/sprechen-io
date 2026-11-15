using Grpc.Core;
using Sprechen.Proto;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using GrpcSpeechConversationEvent = Sprechen.Proto.SpeechConversationEventMessage;
using AppSupportedLanguage = Sprechen.Speech.LanguageContracts.SupportedLanguage;
using AppSupportedLanguages = Sprechen.Speech.LanguageContracts.SupportedLanguages;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class LanguageConfirmationStreamingSessionTests
{
    [Fact]
    public async Task StopAsync_WhenStreamingCompleted_ReturnsLanguageAndTranscript()
    {
        await using var chunkSource = new ControlledSpeechAudioChunkSource(SpeechAudioMimeType.Pcm);

        var speechRequestWriter = new CollectingClientStreamWriter<AudioChunk>();
        var speechResponseReader = new SequenceAsyncStreamReader<GrpcSpeechConversationEvent>(new[]
        {
            CreateTranscriptUpdate(" first "),
            CreateTranscriptFinalized("second"),
            CreateLanguageLocked("de")
        });
        using var speechCall = new AsyncDuplexStreamingCall<AudioChunk, GrpcSpeechConversationEvent>(
            speechRequestWriter,
            speechResponseReader,
            Task.FromResult(new Metadata()),
            () => new Status(StatusCode.OK, string.Empty),
            () => new Metadata(),
            () => { });

        var supportedLanguages = new AppSupportedLanguages("de", new[]
        {
            new AppSupportedLanguage("de", "Deutsch"),
            new AppSupportedLanguage("en", "English")
        });

        await using var session = new LanguageConfirmationStreamingSession(
            chunkSource,
            speechCall,
            "de",
            supportedLanguages,
            CancellationToken.None);

        chunkSource.PushChunk(new byte[] { 1, 2, 3 });
        chunkSource.PushChunk(new byte[] { 4, 5, 6 });
        chunkSource.Complete();

        await session.StopAsync(CancellationToken.None);
        var result = await session.GetResultAsync(CancellationToken.None);

        Assert.Equal("de", result.Language.IsoCode);
        Assert.Equal("second", result.Transcript);

        Assert.True(speechRequestWriter.Completed);
        Assert.Equal(2, speechRequestWriter.Messages.Count);
        Assert.Equal("de", speechRequestWriter.Messages[0].LanguageCode);
        Assert.True(string.IsNullOrWhiteSpace(speechRequestWriter.Messages[1].LanguageCode));
        Assert.Equal(1, chunkSource.StopCallCount);
    }

    [Fact]
    public async Task StopAsync_WhenSpeechSendLoopWriteIsBlocked_CompletesWithinGraceByCancelling()
    {
        await using var chunkSource = new ControlledSpeechAudioChunkSource(SpeechAudioMimeType.Pcm);
        var speechRequestWriter = new BlockingClientStreamWriter<AudioChunk>();
        using var speechCall = new AsyncDuplexStreamingCall<AudioChunk, GrpcSpeechConversationEvent>(
            speechRequestWriter,
            new SequenceAsyncStreamReader<GrpcSpeechConversationEvent>(Array.Empty<GrpcSpeechConversationEvent>()),
            Task.FromResult(new Metadata()),
            () => new Status(StatusCode.OK, string.Empty),
            () => new Metadata(),
            () => { });

        var supportedLanguages = new AppSupportedLanguages("de", new[]
        {
            new AppSupportedLanguage("de", "Deutsch")
        });

        await using var session = new LanguageConfirmationStreamingSession(
            chunkSource,
            speechCall,
            "de",
            supportedLanguages,
            TimeSpan.FromMilliseconds(200),
            CancellationToken.None);

        chunkSource.PushChunk(new byte[] { 1, 2, 3 });
        await speechRequestWriter.FirstWriteStarted.WaitAsync(TimeSpan.FromSeconds(2));

        // Ein blockierter Schreibaufruf darf Stop nicht dauerhaft aufhalten. Ohne Transkript wird NoSpeech erwartet.
        await Assert.ThrowsAsync<NoSpeechDetectedException>(
            () => session.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task StopAsync_WhenSpeechStreamReturnsNoTranscript_ThrowsNoSpeechDetectedException()
    {
        await using var chunkSource = new ControlledSpeechAudioChunkSource(SpeechAudioMimeType.Pcm);

        var speechRequestWriter = new CollectingClientStreamWriter<AudioChunk>();
        using var speechCall = new AsyncDuplexStreamingCall<AudioChunk, GrpcSpeechConversationEvent>(
            speechRequestWriter,
            new SequenceAsyncStreamReader<GrpcSpeechConversationEvent>(Array.Empty<GrpcSpeechConversationEvent>()),
            Task.FromResult(new Metadata()),
            () => new Status(StatusCode.OK, string.Empty),
            () => new Metadata(),
            () => { });

        var supportedLanguages = new AppSupportedLanguages("de", new[]
        {
            new AppSupportedLanguage("de", "Deutsch")
        });

        await using var session = new LanguageConfirmationStreamingSession(
            chunkSource,
            speechCall,
            "de",
            supportedLanguages,
            CancellationToken.None);

        chunkSource.PushChunk(new byte[] { 1, 2, 3 });
        chunkSource.Complete();

        var exception = await Assert.ThrowsAsync<NoSpeechDetectedException>(() => session.StopAsync(CancellationToken.None));

        Assert.Contains("no speech", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StopAsync_WhenLanguageResultIsMissing_ThrowsInvalidOperationException()
    {
        await using var chunkSource = new ControlledSpeechAudioChunkSource(SpeechAudioMimeType.Pcm);

        var speechRequestWriter = new CollectingClientStreamWriter<AudioChunk>();
        using var speechCall = new AsyncDuplexStreamingCall<AudioChunk, GrpcSpeechConversationEvent>(
            speechRequestWriter,
            new SequenceAsyncStreamReader<GrpcSpeechConversationEvent>(new[] { CreateTranscriptFinalized("ok") }),
            Task.FromResult(new Metadata()),
            () => new Status(StatusCode.OK, string.Empty),
            () => new Metadata(),
            () => { });

        var supportedLanguages = new AppSupportedLanguages("de", new[]
        {
            new AppSupportedLanguage("de", "Deutsch")
        });

        await using var session = new LanguageConfirmationStreamingSession(
            chunkSource,
            speechCall,
            null,
            supportedLanguages,
            CancellationToken.None);

        chunkSource.PushChunk(new byte[] { 1, 2, 3 });
        chunkSource.Complete();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => session.StopAsync(CancellationToken.None));

        Assert.Contains("no language result", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StopAsync_WhenDisposedDuringStop_DoesNotFailBecauseSessionStateWasCleared()
    {
        await using var chunkSource = new ControlledSpeechAudioChunkSource(SpeechAudioMimeType.Pcm)
        {
            AutoCompleteOnStop = false
        };

        var speechRequestWriter = new CollectingClientStreamWriter<AudioChunk>();
        using var speechCall = new AsyncDuplexStreamingCall<AudioChunk, GrpcSpeechConversationEvent>(
            speechRequestWriter,
            new SequenceAsyncStreamReader<GrpcSpeechConversationEvent>(new[]
            {
                CreateTranscriptFinalized("ok"),
                CreateLanguageLocked("de")
            }),
            Task.FromResult(new Metadata()),
            () => new Status(StatusCode.OK, string.Empty),
            () => new Metadata(),
            () => { });

        var supportedLanguages = new AppSupportedLanguages("de", new[]
        {
            new AppSupportedLanguage("de", "Deutsch")
        });

        var session = new LanguageConfirmationStreamingSession(
            chunkSource,
            speechCall,
            "de",
            supportedLanguages,
            CancellationToken.None);

        var stopTask = session.StopAsync(CancellationToken.None);
        await Task.Yield();

        var disposeTask = session.DisposeAsync().AsTask();
        chunkSource.Complete();

        await stopTask;
        await disposeTask;
    }

    private static SpeechConversationEventMessage CreateTranscriptUpdate(string text)
    {
        return new SpeechConversationEventMessage
        {
            TranscriptUpdate = new SpeechConversationTranscriptUpdate
            {
                Text = text
            }
        };
    }

    private static SpeechConversationEventMessage CreateTranscriptFinalized(string text)
    {
        return new SpeechConversationEventMessage
        {
            TranscriptFinalized = new SpeechConversationTranscriptFinalized
            {
                Text = text
            }
        };
    }

    private static SpeechConversationEventMessage CreateLanguageLocked(string languageCode)
    {
        return new SpeechConversationEventMessage
        {
            LanguageLocked = new SpeechConversationLanguageLocked
            {
                LanguageCode = languageCode
            }
        };
    }
}


