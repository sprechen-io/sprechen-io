using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Core.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Web.HostSupport.Grpc.Adapter;
using Sprechen.Web.HostSupport.Tests.TestDoubles;
using Xunit;
using SprechenProto = Sprechen.Proto;
using Sprechen.Proto.Transport;

namespace Sprechen.Web.HostSupport.Tests.Grpc;

public sealed class SpeechProxyServiceTests
{
    [Fact]
    public async Task GetSupportedLanguages_ForwardsRequestToBackend()
    {
        Empty? forwardedRequest = null;
        var forwardedCancellationToken = CancellationToken.None;
        var backendResponse = new SprechenProto.SupportedLanguagesResponse
        {
            DefaultIsoCode = "de",
            Languages =
            {
                new SprechenProto.SupportedLanguage
                {
                    IsoCode = "de",
                    DisplayName = "Deutsch"
                },
                new SprechenProto.SupportedLanguage
                {
                    IsoCode = "en",
                    DisplayName = "English"
                }
            }
        };

        var backend = new FakeSupportedLanguagesClient((request, cancellationToken) =>
        {
            forwardedRequest = request;
            forwardedCancellationToken = cancellationToken;
            return backendResponse;
        });

        using var cancellationSource = new CancellationTokenSource();
        var proxy = new SpeechProxyService(backend, NullLogger<SpeechProxyService>.Instance);
        var request = new Empty();

        var response = await proxy.GetSupportedLanguages(
            request,
            CreateContext("GetSupportedLanguages", cancellationSource.Token));

        Assert.Same(backendResponse, response);
        Assert.Same(request, forwardedRequest);
        Assert.Equal(cancellationSource.Token, forwardedCancellationToken);
        Assert.Equal("de", response.DefaultIsoCode);
        Assert.Equal(["de", "en"], response.Languages.Select(language => language.IsoCode));
    }

    [Fact]
    public async Task TextToSpeechToStream_BackendCancelled_DoesNotThrow()
    {
        var chunk = new SprechenProto.AudioResponse
        {
            MimeType = SprechenProto.AudioMimeType.AudioPcm,
            SampleRateHz = 24_000,
            Data = ByteString.CopyFrom(new byte[] { 0x1, 0x2 })
        };

        var cancelled = new RpcException(new Status(StatusCode.Cancelled, "Call canceled by the client."));

        var backend = new FakeLanguageClient((_, _) =>
        {
            var responseStream = new SequenceAsyncStreamReader<SprechenProto.AudioResponse>(
                new[] { chunk },
                exceptionAfterItems: cancelled);

            return new AsyncServerStreamingCall<SprechenProto.AudioResponse>(
                responseStream,
                Task.FromResult(new Metadata()),
                static () => new Status(StatusCode.OK, string.Empty),
                static () => new Metadata(),
                static () => { });
        });

        var logger = new RecordingLogger<SpeechProxyService>();
        var proxy = new SpeechProxyService(backend, logger);
        var responseWriter = new RecordingServerStreamWriter<SprechenProto.AudioResponse>();
        var context = CreateContext("TextToSpeechToStream");

        var request = new SprechenProto.TextRequest
        {
            Text = "hi",
            LanguageCode = "de",
            DesiredMimeType = SprechenProto.AudioMimeType.AudioPcm
        };

        await proxy.TextToSpeechToStream(request, responseWriter, context);

        var response = Assert.Single(responseWriter.Items);
        Assert.Equal(SprechenProto.AudioMimeType.AudioPcm, response.MimeType);
        Assert.Equal(24_000, response.SampleRateHz);
        Assert.Equal(new byte[] { 0x1, 0x2 }, response.Data.ToByteArray());
    }

    [Fact]
    public async Task TextToSpeechToStream_ClientDisconnectDuringWrite_DoesNotThrow()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var chunk = new SprechenProto.AudioResponse
        {
            MimeType = SprechenProto.AudioMimeType.AudioPcm,
            SampleRateHz = 24_000,
            Data = ByteString.CopyFrom(new byte[] { 0x1, 0x2 })
        };

        var backend = new FakeLanguageClient((_, _) =>
        {
            var responseStream = new SequenceAsyncStreamReader<SprechenProto.AudioResponse>(
                new[] { chunk });

            return new AsyncServerStreamingCall<SprechenProto.AudioResponse>(
                responseStream,
                Task.FromResult(new Metadata()),
                static () => new Status(StatusCode.OK, string.Empty),
                static () => new Metadata(),
                static () => { });
        });

        var logger = new RecordingLogger<SpeechProxyService>();
        var proxy = new SpeechProxyService(backend, logger);
        var responseWriter = new ThrowingServerStreamWriter<SprechenProto.AudioResponse>();
        var context = CreateContext("TextToSpeechToStream", cancellationSource.Token);

        var request = new SprechenProto.TextRequest
        {
            Text = "hi",
            LanguageCode = "de",
            DesiredMimeType = SprechenProto.AudioMimeType.AudioPcm
        };

        await proxy.TextToSpeechToStream(request, responseWriter, context);
    }

    [Fact]
    public async Task TextToSpeechToStream_WriteFailsWithoutCancellation_Throws()
    {
        var chunk = new SprechenProto.AudioResponse
        {
            MimeType = SprechenProto.AudioMimeType.AudioPcm,
            SampleRateHz = 24_000,
            Data = ByteString.CopyFrom(new byte[] { 0x1, 0x2 })
        };

        var backend = new FakeLanguageClient((_, _) =>
        {
            var responseStream = new SequenceAsyncStreamReader<SprechenProto.AudioResponse>(
                new[] { chunk });

            return new AsyncServerStreamingCall<SprechenProto.AudioResponse>(
                responseStream,
                Task.FromResult(new Metadata()),
                static () => new Status(StatusCode.OK, string.Empty),
                static () => new Metadata(),
                static () => { });
        });

        var proxy = new SpeechProxyService(backend, NullLogger<SpeechProxyService>.Instance);
        var responseWriter = new ThrowingServerStreamWriter<SprechenProto.AudioResponse>();
        var context = CreateContext("TextToSpeechToStream");

        var request = new SprechenProto.TextRequest
        {
            Text = "hi",
            LanguageCode = "de",
            DesiredMimeType = SprechenProto.AudioMimeType.AudioPcm
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => proxy.TextToSpeechToStream(request, responseWriter, context));
    }

    [Fact]
    public async Task TextToSpeechToStream_BackendCodedError_PropagatesErrorCode()
    {
        var backendError = new RpcException(
            new Status(StatusCode.InvalidArgument, "Text is required."),
            SprechenGrpcErrorMetadata.CreateTrailers(SprechenProto.SprechenErrorCode.TextRequired));

        var backend = new FakeLanguageClient((_, _) =>
        {
            var responseStream = new SequenceAsyncStreamReader<SprechenProto.AudioResponse>(
                Array.Empty<SprechenProto.AudioResponse>(),
                exceptionAfterItems: backendError);

            return new AsyncServerStreamingCall<SprechenProto.AudioResponse>(
                responseStream,
                Task.FromResult(new Metadata()),
                static () => new Status(StatusCode.OK, string.Empty),
                static () => new Metadata(),
                static () => { });
        });

        var proxy = new SpeechProxyService(backend, NullLogger<SpeechProxyService>.Instance);
        var responseWriter = new RecordingServerStreamWriter<SprechenProto.AudioResponse>();
        var context = CreateContext("TextToSpeechToStream");

        var request = new SprechenProto.TextRequest
        {
            Text = " ",
            LanguageCode = "de",
            DesiredMimeType = SprechenProto.AudioMimeType.AudioPcm
        };

        var ex = await Assert.ThrowsAsync<RpcException>(() => proxy.TextToSpeechToStream(request, responseWriter, context));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        Assert.True(SprechenGrpcErrorMetadata.TryGetErrorCode(ex, out var code));
        Assert.Equal(SprechenProto.SprechenErrorCode.TextRequired, code);
    }

    [Fact]
    public async Task TextToSpeechToStream_BackendCodedError_CopiesErrorCodeOntoResponseTrailers()
    {

        // Der Test ruft den Proxy direkt auf; ein Transport bis zum Browser wird hier nicht ausgeführt.
        var backendError = new RpcException(
            new Status(StatusCode.InvalidArgument, "Text is required."),
            SprechenGrpcErrorMetadata.CreateTrailers(SprechenProto.SprechenErrorCode.TextRequired));

        var backend = new FakeLanguageClient((_, _) =>
        {
            var responseStream = new SequenceAsyncStreamReader<SprechenProto.AudioResponse>(
                Array.Empty<SprechenProto.AudioResponse>(),
                exceptionAfterItems: backendError);

            return new AsyncServerStreamingCall<SprechenProto.AudioResponse>(
                responseStream,
                Task.FromResult(new Metadata()),
                static () => new Status(StatusCode.OK, string.Empty),
                static () => new Metadata(),
                static () => { });
        });

        var proxy = new SpeechProxyService(backend, NullLogger<SpeechProxyService>.Instance);
        var responseWriter = new RecordingServerStreamWriter<SprechenProto.AudioResponse>();
        var context = CreateContext("TextToSpeechToStream");
        var request = new SprechenProto.TextRequest
        {
            Text = " ",
            LanguageCode = "de",
            DesiredMimeType = SprechenProto.AudioMimeType.AudioPcm
        };

        await Assert.ThrowsAsync<RpcException>(() => proxy.TextToSpeechToStream(request, responseWriter, context));

        Assert.True(
            SprechenGrpcErrorMetadata.TryGetErrorCode(context.ResponseTrailers, out var trailerCode),
            "Expected the proxy to copy the backend error code onto context.ResponseTrailers (the wire-visible channel).");
        Assert.Equal(SprechenProto.SprechenErrorCode.TextRequired, trailerCode);
    }

    [Fact]
    public async Task TextToSpeechToStream_BackendReturnsNoAudio_ThrowsCodedProviderFailure()
    {
        var backend = new FakeLanguageClient((_, _) =>
        {
            var responseStream = new SequenceAsyncStreamReader<SprechenProto.AudioResponse>(
                Array.Empty<SprechenProto.AudioResponse>());

            return new AsyncServerStreamingCall<SprechenProto.AudioResponse>(
                responseStream,
                Task.FromResult(new Metadata()),
                static () => new Status(StatusCode.OK, string.Empty),
                static () => new Metadata(),
                static () => { });
        });

        var proxy = new SpeechProxyService(backend, NullLogger<SpeechProxyService>.Instance);
        var responseWriter = new RecordingServerStreamWriter<SprechenProto.AudioResponse>();
        var context = CreateContext("TextToSpeechToStream");

        var request = new SprechenProto.TextRequest
        {
            Text = "hi",
            LanguageCode = "de",
            DesiredMimeType = SprechenProto.AudioMimeType.AudioPcm
        };

        var ex = await Assert.ThrowsAsync<RpcException>(() => proxy.TextToSpeechToStream(request, responseWriter, context));

        Assert.Equal(StatusCode.Internal, ex.StatusCode);
        Assert.True(SprechenGrpcErrorMetadata.TryGetErrorCode(ex, out var code));
        Assert.Equal(SprechenProto.SprechenErrorCode.ProviderFailure, code);
    }

    [Fact]
    public async Task SpeechToText_BackendReturnsNoTranscript_ReturnsEmptyTranscript()
    {
        var requestWriter = new CollectingClientStreamWriter<SprechenProto.AudioChunk>();
        var backend = new FakeLanguageClient(
            textToSpeechToStream: null,
            speechToText: _ => new AsyncClientStreamingCall<SprechenProto.AudioChunk, SprechenProto.SpeechTranscript>(
                requestWriter,
                Task.FromResult(new SprechenProto.SpeechTranscript()),
                Task.FromResult(new Metadata()),
                static () => new Status(StatusCode.OK, string.Empty),
                static () => new Metadata(),
                static () => { }),
            speechToTextStream: null,
            speechToConversationStream: null);

        var logger = new RecordingLogger<SpeechProxyService>();
        var proxy = new SpeechProxyService(backend, logger);
        var requestStream = new SequenceAsyncStreamReader<SprechenProto.AudioChunk>(new[]
        {
            new SprechenProto.AudioChunk
            {
                Data = ByteString.CopyFrom(new byte[] { 0x1 }),
                MimeType = SprechenProto.AudioMimeType.AudioPcm,
                LanguageCode = "de"
            }
        });

        var response = await proxy.SpeechToText(requestStream, CreateContext("SpeechToText"));

        Assert.Equal(string.Empty, response.Text);
        Assert.False(response.HasConfidence);
    }

    [Fact]
    public async Task SpeechToTextStream_BackendReturnsNoTranscript_ForwardsEmptyTranscript()
    {
        var requestWriter = new CollectingClientStreamWriter<SprechenProto.AudioChunk>();
        var backend = new FakeLanguageClient(
            textToSpeechToStream: null,
            speechToText: null,
            speechToTextStream: _ => new AsyncDuplexStreamingCall<SprechenProto.AudioChunk, SprechenProto.SpeechTranscript>(
                requestWriter,
                new SequenceAsyncStreamReader<SprechenProto.SpeechTranscript>(new[]
                {
                    new SprechenProto.SpeechTranscript()
                }),
                Task.FromResult(new Metadata()),
                static () => new Status(StatusCode.OK, string.Empty),
                static () => new Metadata(),
                static () => { }),
            speechToConversationStream: null);

        var logger = new RecordingLogger<SpeechProxyService>();
        var proxy = new SpeechProxyService(backend, logger);
        var requestStream = new SequenceAsyncStreamReader<SprechenProto.AudioChunk>(new[]
        {
            new SprechenProto.AudioChunk
            {
                Data = ByteString.CopyFrom(new byte[] { 0x1 }),
                MimeType = SprechenProto.AudioMimeType.AudioPcm,
                LanguageCode = "de"
            }
        });
        var responseWriter = new RecordingServerStreamWriter<SprechenProto.SpeechTranscript>();

        await proxy.SpeechToTextStream(requestStream, responseWriter, CreateContext("SpeechToTextStream"));

        var response = Assert.Single(responseWriter.Items);
        Assert.Equal(string.Empty, response.Text);
        Assert.False(response.HasConfidence);
    }

    [Fact]
    public async Task SpeechToConversationStream_ForwardsConversationEventsAndRequests()
    {
        var requestWriter = new CollectingClientStreamWriter<SprechenProto.AudioChunk>();
        var backend = new FakeLanguageClient(
            textToSpeechToStream: null,
            speechToText: null,
            speechToTextStream: null,
            speechToConversationStream: _ => new AsyncDuplexStreamingCall<SprechenProto.AudioChunk, SprechenProto.SpeechConversationEventMessage>(
                requestWriter,
                new SequenceAsyncStreamReader<SprechenProto.SpeechConversationEventMessage>(new[]
                {
                    new SprechenProto.SpeechConversationEventMessage
                    {
                        TranscriptFinalized = new SprechenProto.SpeechConversationTranscriptFinalized
                        {
                            Text = "ok"
                        }
                    },
                    new SprechenProto.SpeechConversationEventMessage
                    {
                        LanguageLocked = new SprechenProto.SpeechConversationLanguageLocked
                        {
                            LanguageCode = "de"
                        }
                    }
                }),
                Task.FromResult(new Metadata()),
                static () => new Status(StatusCode.OK, string.Empty),
                static () => new Metadata(),
                static () => { }));

        var logger = new RecordingLogger<SpeechProxyService>();
        var proxy = new SpeechProxyService(backend, logger);
        var requestStream = new SequenceAsyncStreamReader<SprechenProto.AudioChunk>(new[]
        {
            new SprechenProto.AudioChunk
            {
                Data = ByteString.CopyFrom(new byte[] { 0x1 }),
                MimeType = SprechenProto.AudioMimeType.AudioPcm,
                LanguageCode = "de"
            }
        });
        var responseWriter = new RecordingServerStreamWriter<SprechenProto.SpeechConversationEventMessage>();
        var requestHeaders = new Metadata
        {
            { SprechenGrpcCorrelationMetadata.CorrelationIdHeaderName, "corr-123" }
        };

        await proxy.SpeechToConversationStream(
            requestStream,
            responseWriter,
            CreateContext("SpeechToConversationStream", CancellationToken.None, requestHeaders));

        Assert.Single(requestWriter.Messages);
        Assert.Equal("de", requestWriter.Messages[0].LanguageCode);
        Assert.True(SprechenGrpcCorrelationMetadata.TryGetCorrelationId(backend.LastSpeechToConversationStreamHeaders, out var correlationId));
        Assert.Equal("corr-123", correlationId);
        Assert.Collection(
            responseWriter.Items,
            item => Assert.Equal("ok", item.TranscriptFinalized.Text),
            item => Assert.Equal("de", item.LanguageLocked.LanguageCode));
        Assert.Contains(
            logger.Entries,
            entry => entry.Message.Contains("Proxy SpeechToConversationStream started", StringComparison.Ordinal)
                && entry.Message.Contains("corr-123", StringComparison.Ordinal));
        Assert.Contains(
            logger.Entries,
            entry => entry.Message.Contains("Proxy SpeechToConversationStream completed", StringComparison.Ordinal)
                && entry.Message.Contains("corr-123", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SpeechToConversationStream_WhenBackendResponseIsCancelled_LogsCancellationStage()
    {
        var requestWriter = new CollectingClientStreamWriter<SprechenProto.AudioChunk>();
        var cancelled = new RpcException(new Status(StatusCode.Cancelled, "cancelled"));
        var backend = new FakeLanguageClient(
            textToSpeechToStream: null,
            speechToText: null,
            speechToTextStream: null,
            speechToConversationStream: _ => new AsyncDuplexStreamingCall<SprechenProto.AudioChunk, SprechenProto.SpeechConversationEventMessage>(
                requestWriter,
                new SequenceAsyncStreamReader<SprechenProto.SpeechConversationEventMessage>(
                    Array.Empty<SprechenProto.SpeechConversationEventMessage>(),
                    exceptionAfterItems: cancelled),
                Task.FromResult(new Metadata()),
                static () => new Status(StatusCode.OK, string.Empty),
                static () => new Metadata(),
                static () => { }));

        var logger = new RecordingLogger<SpeechProxyService>();
        var proxy = new SpeechProxyService(backend, logger);
        var requestStream = new SequenceAsyncStreamReader<SprechenProto.AudioChunk>(new[]
        {
            new SprechenProto.AudioChunk
            {
                Data = ByteString.CopyFrom(new byte[] { 0x1 }),
                MimeType = SprechenProto.AudioMimeType.AudioPcm,
                LanguageCode = "de"
            }
        });
        var responseWriter = new RecordingServerStreamWriter<SprechenProto.SpeechConversationEventMessage>();

        await proxy.SpeechToConversationStream(requestStream, responseWriter, CreateContext("SpeechToConversationStream"));

        Assert.Contains(
            logger.Entries,
            entry => entry.Message.Contains("canceled at stage proxy-response-stream", StringComparison.Ordinal)
                && entry.Message.Contains("SpeechToConversationStream", StringComparison.Ordinal));
    }

    private static ServerCallContext CreateContext(string method)
    {
        return CreateContext(method, CancellationToken.None);
    }

    private static ServerCallContext CreateContext(string method, CancellationToken cancellationToken) =>
        CreateContext(method, cancellationToken, new Metadata());

    private static ServerCallContext CreateContext(string method, CancellationToken cancellationToken, Metadata requestHeaders) =>
        TestServerCallContext.Create(
            method,
            null,
            DateTime.UtcNow.AddMinutes(1),
            requestHeaders,
            cancellationToken,
            "127.0.0.1",
            null,
            null,
            _ => Task.CompletedTask,
            () => new WriteOptions(),
            _ => { }
        );
}
