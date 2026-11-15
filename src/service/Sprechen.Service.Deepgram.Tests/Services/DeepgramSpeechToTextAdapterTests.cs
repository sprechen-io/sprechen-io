using global::Deepgram.Models.Exceptions.v1;
using global::Deepgram.Models.Listen.v2.WebSocket;
using Microsoft.Extensions.Logging;
using Moq;
using Sprechen.Service.Deepgram.Services;
using Sprechen.Service.Deepgram.Tests.TestDoubles;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Xunit;
using DeepgramListenRestClient = global::Deepgram.Clients.Interfaces.v1.IListenRESTClient;
using DeepgramListenWebSocketClient = global::Deepgram.Clients.Interfaces.v2.IListenWebSocketClient;

namespace Sprechen.Service.Deepgram.Tests.Services;

public sealed class DeepgramSpeechToTextAdapterTests
{
    [Fact]
    public async Task TranscribeStreamingAsync_EmptyAudio_ReturnsEmptyTranscriptWithoutOpeningClients()
    {
        var restClient = new Mock<DeepgramListenRestClient>(MockBehavior.Strict);
        var adapter = CreateAdapter(restClient.Object, CreateListenFactoryThatMustNotBeUsed());

        var result = await adapter.TranscribeStreamingAsync(
            SingleChunk(Array.Empty<byte>()),
            AudioMimeType.Pcm,
            CancellationToken.None);

        Assert.Equal(string.Empty, result.Text);
        Assert.Null(result.ConfidencePercent);
        restClient.VerifyAll();
    }

    [Fact]
    public async Task TranscribeStreamingAsync_Pcm_UsesPrerecordedRestClientAndReturnsTranscript()
    {
        byte[]? requestPayload = null;
        global::Deepgram.Models.Listen.v1.REST.PreRecordedSchema? requestSchema = null;
        Dictionary<string, string>? requestAddons = null;

        var restClient = new Mock<DeepgramListenRestClient>(MockBehavior.Strict);
        restClient.Setup(c => c.TranscribeFile(
                It.IsAny<byte[]>(),
                It.IsAny<global::Deepgram.Models.Listen.v1.REST.PreRecordedSchema>(),
                It.IsAny<CancellationTokenSource?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<Dictionary<string, string>?>()))
            .Callback<byte[], global::Deepgram.Models.Listen.v1.REST.PreRecordedSchema, CancellationTokenSource?, Dictionary<string, string>?, Dictionary<string, string>?>((payload, schema, _, addons, _) =>
            {
                requestPayload = payload;
                requestSchema = schema;
                requestAddons = addons;
            })
            .ReturnsAsync(new global::Deepgram.Models.Listen.v1.REST.SyncResponse
            {
                Results = new global::Deepgram.Models.Listen.v1.REST.Results
                {
                    Channels = new[]
                    {
                        new global::Deepgram.Models.Listen.v1.REST.Channel
                        {
                            Alternatives = new[]
                            {
                                new global::Deepgram.Models.Listen.v1.REST.Alternative
                                {
                                    Transcript = "Okay, Hilfe.",
                                    Confidence = 0.981
                                }
                            }
                        }
                    }
                }
            });

        var (adapter, logger) = CreateAdapterWithLogger(restClient.Object, CreateListenFactoryThatMustNotBeUsed());

        var transcript = await adapter.TranscribeStreamingAsync(
            SingleChunk(PcmForDurationMs(300)),
            AudioMimeType.Pcm,
            CancellationToken.None,
            languageCode: " de ");

        Assert.Equal("Okay, Hilfe.", transcript.Text);
        Assert.Equal(98, transcript.ConfidencePercent);

        Assert.NotNull(requestSchema);
        Assert.Equal("nova-3-general", requestSchema!.Model);
        Assert.False(requestSchema.DetectLanguage);
        Assert.Equal("de", requestSchema.Language);
        Assert.True(requestSchema.Punctuate);
        Assert.True(requestSchema.SmartFormat);

        Assert.NotNull(requestAddons);
        Assert.Equal("true", requestAddons!["mip_opt_out"]);

        Assert.NotNull(requestPayload);
        Assert.True(requestPayload!.Length > 44);
        Assert.Equal((byte)'R', requestPayload[0]);
        Assert.Equal((byte)'I', requestPayload[1]);
        Assert.Equal((byte)'F', requestPayload[2]);
        Assert.Equal((byte)'F', requestPayload[3]);

        Assert.Contains(
            logger.Entries,
            entry => entry.Message.Contains("Deepgram prerecorded transcription completed", StringComparison.Ordinal)
                && entry.Message.Contains("providerMs=", StringComparison.Ordinal)
                && entry.Message.Contains("transcriptLength=12", StringComparison.Ordinal));
        Assert.DoesNotContain(
            logger.Entries,
            entry => entry.Level == LogLevel.Information && entry.Message.Contains("Okay", StringComparison.Ordinal));

        restClient.VerifyAll();
    }

    [Fact]
    public async Task TranscribeStreamingAsync_WithClientCorrelationId_WritesItIntoTheCompletedRecord()
    {
        var restClient = new Mock<DeepgramListenRestClient>(MockBehavior.Strict);
        restClient.Setup(c => c.TranscribeFile(
                It.IsAny<byte[]>(),
                It.IsAny<global::Deepgram.Models.Listen.v1.REST.PreRecordedSchema>(),
                It.IsAny<CancellationTokenSource?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<Dictionary<string, string>?>()))
            .ReturnsAsync(new global::Deepgram.Models.Listen.v1.REST.SyncResponse
            {
                Results = new global::Deepgram.Models.Listen.v1.REST.Results
                {
                    Channels = new[]
                    {
                        new global::Deepgram.Models.Listen.v1.REST.Channel
                        {
                            Alternatives = new[]
                            {
                                new global::Deepgram.Models.Listen.v1.REST.Alternative
                                {
                                    Transcript = "Hallo.",
                                    Confidence = 0.95
                                }
                            }
                        }
                    }
                }
            });

        var (adapter, logger) = CreateAdapterWithLogger(restClient.Object, CreateListenFactoryThatMustNotBeUsed());

        await adapter.TranscribeStreamingAsync(
            SingleChunk(PcmForDurationMs(300)),
            AudioMimeType.Pcm,
            CancellationToken.None,
            languageCode: "de",
            clientCorrelationId: "turn-1a2b3c");

        // Der Abschlusslog muss die Korrelationskennung als strukturierte Eigenschaft und im formatierten Text enthalten.
        var completed = Assert.Single(
            logger.Entries,
            entry => entry.Level == LogLevel.Information
                && entry.Message.Contains("Deepgram prerecorded transcription completed", StringComparison.Ordinal));
        Assert.Equal("turn-1a2b3c", completed.Properties["ClientCorrelationId"]);
        Assert.Contains("clientCorrelationId=turn-1a2b3c", completed.Message, StringComparison.Ordinal);

        restClient.VerifyAll();
    }

    [Fact]
    public async Task TranscribeStreamingAsync_WithContext_LogsExactOutgoingKeytermListAtInformation()
    {
        const string firstPrivateContext = "PRIVATE_CONTEXT_ALPHA_9QZ";
        const string secondPrivateContext = "PRIVATE_CONTEXT_BETA_7WX";
        global::Deepgram.Models.Listen.v1.REST.PreRecordedSchema? requestSchema = null;

        var restClient = new Mock<DeepgramListenRestClient>(MockBehavior.Strict);
        restClient.Setup(c => c.TranscribeFile(
                It.IsAny<byte[]>(),
                It.IsAny<global::Deepgram.Models.Listen.v1.REST.PreRecordedSchema>(),
                It.IsAny<CancellationTokenSource?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<Dictionary<string, string>?>()))
            .Callback<byte[], global::Deepgram.Models.Listen.v1.REST.PreRecordedSchema, CancellationTokenSource?, Dictionary<string, string>?, Dictionary<string, string>?>((_, schema, _, _, _) =>
            {
                requestSchema = schema;
            })
            .ReturnsAsync(new global::Deepgram.Models.Listen.v1.REST.SyncResponse
            {
                Metadata = new global::Deepgram.Models.Listen.v1.REST.Metadata { RequestId = "dg-request-safe-42" },
                Results = new global::Deepgram.Models.Listen.v1.REST.Results
                {
                    Channels =
                    [
                        new global::Deepgram.Models.Listen.v1.REST.Channel
                        {
                            Alternatives =
                            [
                                new global::Deepgram.Models.Listen.v1.REST.Alternative
                                {
                                    Transcript = "Ja",
                                    Confidence = 0.68
                                }
                            ]
                        }
                    ]
                }
            });

        var (adapter, logger) = CreateAdapterWithLogger(restClient.Object, CreateListenFactoryThatMustNotBeUsed());
        using var activity = new Activity("deepgram-keyterm-observability-test")
            .SetIdFormat(ActivityIdFormat.W3C)
            .Start();
        var keyterms = new[]
        {
            firstPrivateContext,
            firstPrivateContext,
            secondPrivateContext
        };

        var transcript = await adapter.TranscribeStreamingAsync(
            SingleChunk(PcmForDurationMs(300)),
            AudioMimeType.Pcm,
            CancellationToken.None,
            languageCode: "de",
            recognitionProfile: RecognitionProfile.Conversational,
            keyterms: new SpeechKeyterms(keyterms, KeytermComposition.WithServerBase));

        // Die protokollierte Begriffsliste muss der im Anbieter-Testdoppel erfassten Liste in derselben Reihenfolge entsprechen.
        Assert.Equal("Ja", transcript.Text);
        Assert.Equal(68, transcript.ConfidencePercent);
        Assert.NotNull(requestSchema?.Keyterm);
        Assert.Contains("ja", requestSchema!.Keyterm!);
        Assert.Contains(firstPrivateContext, requestSchema.Keyterm!);
        Assert.Contains(secondPrivateContext, requestSchema.Keyterm!);

        var completion = Assert.Single(
            logger.Entries,
            entry => entry.Level == LogLevel.Information
                && entry.Message.Contains("Deepgram prerecorded transcription completed", StringComparison.Ordinal));
        Assert.Equal(RecognitionProfile.Conversational, completion.Properties["RecognitionProfile"]);
        Assert.Equal(true, completion.Properties["KeytermParameterPresent"]);
        Assert.Equal(requestSchema.Keyterm!.Count, completion.Properties["KeytermCount"]);
        var loggedKeyterms = JsonSerializer.Deserialize<string[]>(
            Assert.IsType<string>(completion.Properties["KeytermList"]));
        Assert.Equal(requestSchema.Keyterm, loggedKeyterms);
        Assert.Contains(firstPrivateContext, completion.Message, StringComparison.Ordinal);
        Assert.Contains(secondPrivateContext, completion.Message, StringComparison.Ordinal);
        Assert.Contains("\"ja\"", completion.Message, StringComparison.Ordinal);
        Assert.Equal(true, completion.Properties["ConfirmationKeytermsApplied"]);
        Assert.Equal(
            ConfirmationKeytermVocabulary.Default.GetKeyterms("de").Count,
            completion.Properties["ConfirmationKeytermCount"]);
        Assert.Equal(true, completion.Properties["ContextKeytermsRequested"]);
        Assert.Equal(2, completion.Properties["ContextKeytermRequestedCount"]);
        Assert.Equal(true, completion.Properties["ContextKeytermsApplied"]);
        Assert.Equal(2, completion.Properties["ContextKeytermAppliedCount"]);
        Assert.Equal("dg-request-safe-42", completion.Properties["DeepgramRequestId"]);
        Assert.Equal(activity.TraceId.ToString(), completion.Properties["TraceId"]);
        Assert.False(completion.Properties.ContainsKey("Transcript"));
        Assert.False(completion.Properties.ContainsKey("Schema"));
        Assert.False(completion.Properties.ContainsKey("RequestUrl"));

        restClient.VerifyAll();
    }

    [Fact]
    public async Task TranscribeStreamingAsync_ShortPcm_ReturnsEmptyTranscriptWithoutRestCall()
    {
        var restClient = new Mock<DeepgramListenRestClient>(MockBehavior.Strict);
        var (adapter, logger) = CreateAdapterWithLogger(restClient.Object, CreateListenFactoryThatMustNotBeUsed());

        var result = await adapter.TranscribeStreamingAsync(
            SingleChunk(PcmForDurationMs(100)),
            AudioMimeType.Pcm,
            CancellationToken.None,
            languageCode: "de");

        Assert.Equal(string.Empty, result.Text);
        Assert.Null(result.ConfidencePercent);
        Assert.Contains(
            logger.Entries,
            entry => entry.Message.Contains("skipped short PCM segment", StringComparison.Ordinal)
                && entry.Message.Contains("audioDurationMs=100", StringComparison.Ordinal)
                && entry.Message.Contains("minimumDurationMs=250", StringComparison.Ordinal));
        restClient.VerifyAll();
    }

    [Fact]
    public async Task TranscribeStreamingAsync_WhenLanguageMissing_EnablesDeepgramLanguageDetection()
    {
        global::Deepgram.Models.Listen.v1.REST.PreRecordedSchema? requestSchema = null;

        var restClient = new Mock<DeepgramListenRestClient>(MockBehavior.Strict);
        restClient.Setup(c => c.TranscribeFile(
                It.IsAny<byte[]>(),
                It.IsAny<global::Deepgram.Models.Listen.v1.REST.PreRecordedSchema>(),
                It.IsAny<CancellationTokenSource?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<Dictionary<string, string>?>()))
            .Callback<byte[], global::Deepgram.Models.Listen.v1.REST.PreRecordedSchema, CancellationTokenSource?, Dictionary<string, string>?, Dictionary<string, string>?>((_, schema, _, _, _) =>
            {
                requestSchema = schema;
            })
            .ReturnsAsync(new global::Deepgram.Models.Listen.v1.REST.SyncResponse());

        var (adapter, logger) = CreateAdapterWithLogger(restClient.Object, CreateListenFactoryThatMustNotBeUsed());

        await adapter.TranscribeStreamingAsync(
            SingleChunk(PcmForDurationMs(300)),
            AudioMimeType.Pcm,
            CancellationToken.None,
            languageCode: null);

        Assert.NotNull(requestSchema);
        Assert.True(requestSchema!.DetectLanguage);
        Assert.Null(requestSchema.Language);
        restClient.VerifyAll();
    }

    [Fact]
    public async Task TranscribeStreamingAsync_WhenRestClientThrowsDeepgramException_Wraps()
    {
        var deepgramException = new DeepgramException("bad")
        {
            ErrCode = "x",
            ErrMsg = "nope",
            RequestId = "req-1"
        };

        var restClient = new Mock<DeepgramListenRestClient>(MockBehavior.Strict);
        restClient.Setup(c => c.TranscribeFile(
                It.IsAny<byte[]>(),
                It.IsAny<global::Deepgram.Models.Listen.v1.REST.PreRecordedSchema>(),
                It.IsAny<CancellationTokenSource?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<Dictionary<string, string>?>()))
            .ThrowsAsync(deepgramException);

        var (adapter, logger) = CreateAdapterWithLogger(restClient.Object, CreateListenFactoryThatMustNotBeUsed());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.TranscribeStreamingAsync(
                SingleChunk(PcmForDurationMs(300)),
                AudioMimeType.Pcm,
                CancellationToken.None,
                languageCode: "de",
                keyterms: new SpeechKeyterms(["FAILURE_CONTEXT_GENERIC"], KeytermComposition.WithServerBase)));

        Assert.Contains("Deepgram transcription failed", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("req-1", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Same(deepgramException, exception.InnerException);
        AssertFailedKeytermLog(logger, "provider-error", "req-1", "FAILURE_CONTEXT_GENERIC");

        restClient.VerifyAll();
    }

    [Fact]
    public async Task TranscribeStreamingAsync_WhenRestClientThrowsRateLimited_MapsToRateLimitedException()
    {
        var deepgramException = new DeepgramException("Too Many Requests")
        {
            ErrCode = "TOO_MANY_REQUESTS",
            ErrMsg = "Rate limit exceeded (429).",
            RequestId = "req-2"
        };

        var restClient = new Mock<DeepgramListenRestClient>(MockBehavior.Strict);
        restClient.Setup(c => c.TranscribeFile(
                It.IsAny<byte[]>(),
                It.IsAny<global::Deepgram.Models.Listen.v1.REST.PreRecordedSchema>(),
                It.IsAny<CancellationTokenSource?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<Dictionary<string, string>?>()))
            .ThrowsAsync(deepgramException);

        var (adapter, logger) = CreateAdapterWithLogger(restClient.Object, CreateListenFactoryThatMustNotBeUsed());

        var exception = await Assert.ThrowsAsync<SpeechProviderRateLimitedException>(() =>
            adapter.TranscribeStreamingAsync(
                SingleChunk(PcmForDurationMs(300)),
                AudioMimeType.Pcm,
                CancellationToken.None,
                languageCode: "de",
                keyterms: new SpeechKeyterms(["FAILURE_CONTEXT_RATE_LIMIT"], KeytermComposition.WithServerBase)));

        Assert.Equal("Deepgram", exception.Provider);
        Assert.Same(deepgramException, exception.InnerException);
        AssertFailedKeytermLog(logger, "rate-limited", "req-2", "FAILURE_CONTEXT_RATE_LIMIT");

        restClient.VerifyAll();
    }

    [Fact]
    public async Task TranscribeStreamingAsync_WhenResponseEndsPrematurely_Wraps()
    {
        var inner = new HttpIOException(HttpRequestError.ResponseEnded, "premature");
        var httpException = new HttpRequestException("premature", inner);

        var restClient = new Mock<DeepgramListenRestClient>(MockBehavior.Strict);
        restClient.Setup(c => c.TranscribeFile(
                It.IsAny<byte[]>(),
                It.IsAny<global::Deepgram.Models.Listen.v1.REST.PreRecordedSchema>(),
                It.IsAny<CancellationTokenSource?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<Dictionary<string, string>?>()))
            .ThrowsAsync(httpException);

        var (adapter, logger) = CreateAdapterWithLogger(restClient.Object, CreateListenFactoryThatMustNotBeUsed());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.TranscribeStreamingAsync(
                SingleChunk(PcmForDurationMs(300)),
                AudioMimeType.Pcm,
                CancellationToken.None,
                languageCode: "de",
                keyterms: new SpeechKeyterms(["FAILURE_CONTEXT_RESPONSE"], KeytermComposition.WithServerBase)));

        Assert.Contains("response ended prematurely", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Same(httpException, exception.InnerException);
        AssertFailedKeytermLog(logger, "response-ended", "<none>", "FAILURE_CONTEXT_RESPONSE");

        restClient.VerifyAll();
    }

    [Fact]
    public async Task TranscribeStreamingAsync_WhenProviderTimesOut_LogsExactOutgoingKeytermsBeforeRethrow()
    {
        var restClient = new Mock<DeepgramListenRestClient>(MockBehavior.Strict);
        restClient.Setup(c => c.TranscribeFile(
                It.IsAny<byte[]>(),
                It.IsAny<global::Deepgram.Models.Listen.v1.REST.PreRecordedSchema>(),
                It.IsAny<CancellationTokenSource?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<Dictionary<string, string>?>()))
            .ThrowsAsync(new TaskCanceledException("provider timeout"));
        var (adapter, logger) = CreateAdapterWithLogger(
            restClient.Object,
            CreateListenFactoryThatMustNotBeUsed());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            adapter.TranscribeStreamingAsync(
                SingleChunk(PcmForDurationMs(300)),
                AudioMimeType.Pcm,
                CancellationToken.None,
                languageCode: "de",
                keyterms: new SpeechKeyterms(["FAILURE_CONTEXT_TIMEOUT"], KeytermComposition.WithServerBase)));

        AssertFailedKeytermLog(logger, "timeout", "<none>", "FAILURE_CONTEXT_TIMEOUT");
        restClient.VerifyAll();
    }

    [Fact]
    public async Task TranscribeStreamingFeed_InterimAndFinal_EmitsUpdates()
    {
        EventHandler<ResultResponse>? onResult = null;
        EventHandler<CloseResponse>? onClose = null;

        var wsClient = new Mock<DeepgramListenWebSocketClient>(MockBehavior.Strict);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<ResultResponse>>()))
            .Callback<EventHandler<ResultResponse>>(handler => onResult = handler)
            .ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<ErrorResponse>>()))
            .ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<CloseResponse>>()))
            .Callback<EventHandler<CloseResponse>>(handler => onClose = handler)
            .ReturnsAsync(true);
        wsClient.Setup(c => c.Connect(
                It.IsAny<LiveSchema>(),
                It.IsAny<CancellationTokenSource>(),
                It.Is<Dictionary<string, string>>(addons => addons.ContainsKey("mip_opt_out") && addons["mip_opt_out"] == "true"),
                It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(true);
        wsClient.Setup(c => c.Send(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Callback(() =>
            {
                onResult?.Invoke(null, CreateResult("hello", isFinal: false, confidence: 0.81));
                onResult?.Invoke(null, CreateResult("hello", isFinal: false, confidence: 0.81));
            });
        wsClient.Setup(c => c.SendFinalize())
            .Callback(() =>
            {
                onResult?.Invoke(null, CreateResult("hello world", isFinal: true, confidence: 0.92));
                onClose?.Invoke(null, new CloseResponse());
            })
            .Returns(Task.CompletedTask);
        wsClient.Setup(c => c.Stop(It.IsAny<CancellationTokenSource>(), It.IsAny<bool>())).ReturnsAsync(true);

        var wsFactory = new DelegatingDeepgramListenWebSocketClientFactory(() => wsClient.Object);
        var adapter = CreateAdapter(CreateRestClientThatMustNotBeUsed(), wsFactory);

        var emitted = new List<SpeechTranscript>();
        await foreach (var transcript in adapter.TranscribeStreamingFeed(SingleChunk(new byte[] { 1, 2 }), AudioMimeType.Pcm, CancellationToken.None))
        {
            emitted.Add(transcript);
        }

        Assert.Collection(
            emitted,
            first =>
            {
                Assert.Equal("hello", first.Text);
                Assert.Equal(81, first.ConfidencePercent);
            },
            second =>
            {
                Assert.Equal("hello world", second.Text);
                Assert.Equal(92, second.ConfidencePercent);
            });
        Assert.Equal(1, wsFactory.CreateCalls);
        wsClient.VerifyAll();
    }

    [Fact]
    public async Task StreamConversationAsync_Transcripts_EmitsUpdateFinalAndEndOfTurn()
    {
        EventHandler<ResultResponse>? onResult = null;
        EventHandler<CloseResponse>? onClose = null;

        var wsClient = new Mock<DeepgramListenWebSocketClient>(MockBehavior.Strict);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<ResultResponse>>()))
            .Callback<EventHandler<ResultResponse>>(handler => onResult = handler)
            .ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<ErrorResponse>>()))
            .ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<CloseResponse>>()))
            .Callback<EventHandler<CloseResponse>>(handler => onClose = handler)
            .ReturnsAsync(true);
        wsClient.Setup(c => c.Connect(
                It.IsAny<LiveSchema>(),
                It.IsAny<CancellationTokenSource>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(true);
        wsClient.Setup(c => c.Send(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Callback(() => onResult?.Invoke(null, CreateResult("hello", isFinal: false, confidence: 0.75)));
        wsClient.Setup(c => c.SendFinalize())
            .Callback(() =>
            {
                onResult?.Invoke(null, CreateResult("hello world", isFinal: true, confidence: 0.89));
                onClose?.Invoke(null, new CloseResponse());
            })
            .Returns(Task.CompletedTask);
        wsClient.Setup(c => c.Stop(It.IsAny<CancellationTokenSource>(), It.IsAny<bool>())).ReturnsAsync(true);

        var adapter = CreateAdapter(
            CreateRestClientThatMustNotBeUsed(),
            new DelegatingDeepgramListenWebSocketClientFactory(() => wsClient.Object));

        var events = new List<SpeechConversationEvent>();
        await foreach (var conversationEvent in adapter.StreamConversationAsync(
                           SingleChunk(new byte[] { 1, 2 }),
                           AudioMimeType.Pcm,
                           CancellationToken.None,
                           languageCode: "de"))
        {
            events.Add(conversationEvent);
        }

        Assert.Collection(
            events,
            first =>
            {
                var update = Assert.IsType<SpeechConversationTranscriptUpdateEvent>(first);
                Assert.Equal("hello", update.Transcript);
                Assert.Equal(75, update.Confidence);
                Assert.Equal(0, update.TurnIndex);
            },
            second =>
            {
                var update = Assert.IsType<SpeechConversationTranscriptUpdateEvent>(second);
                Assert.Equal("hello world", update.Transcript);
                Assert.Equal(89, update.Confidence);
                Assert.Equal(0, update.TurnIndex);
            },
            third =>
            {
                var finalized = Assert.IsType<SpeechConversationTranscriptFinalizedEvent>(third);
                Assert.Equal("hello world", finalized.Transcript);
                Assert.Equal(89, finalized.Confidence);
                Assert.Equal(0, finalized.TurnIndex);
            },
            fourth =>
            {
                var endOfTurn = Assert.IsType<SpeechConversationEndOfTurnEvent>(fourth);
                Assert.Equal(0, endOfTurn.TurnIndex);
                Assert.Equal("hello world", endOfTurn.Transcript);
            });
        wsClient.VerifyAll();
    }

    [Fact]
    public async Task TranscribeStreamingFeed_WhenConsumerStopsEarly_StopsWebSocket()
    {
        EventHandler<ResultResponse>? onResult = null;

        var wsClient = new Mock<DeepgramListenWebSocketClient>(MockBehavior.Strict);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<ResultResponse>>()))
            .Callback<EventHandler<ResultResponse>>(handler => onResult = handler)
            .ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<ErrorResponse>>()))
            .ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<CloseResponse>>()))
            .ReturnsAsync(true);
        wsClient.Setup(c => c.Connect(
                It.IsAny<LiveSchema>(),
                It.IsAny<CancellationTokenSource>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(true);
        wsClient.Setup(c => c.Send(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Callback(() => onResult?.Invoke(null, CreateResult("hello", isFinal: false, confidence: 0.75)));
        wsClient.Setup(c => c.Stop(It.IsAny<CancellationTokenSource>(), It.IsAny<bool>()))
            .ReturnsAsync(true);

        var wsFactory = new DelegatingDeepgramListenWebSocketClientFactory(() => wsClient.Object);
        var adapter = CreateAdapter(CreateRestClientThatMustNotBeUsed(), wsFactory);

        var emitted = new List<SpeechTranscript>();
        await foreach (var transcript in adapter.TranscribeStreamingFeed(
                           SingleChunkThenWait(new byte[] { 1, 2 }),
                           AudioMimeType.Pcm,
                           CancellationToken.None))
        {
            emitted.Add(transcript);
            break;
        }

        Assert.Single(emitted);
        Assert.Equal("hello", emitted[0].Text);
        Assert.Equal(1, wsFactory.CreateCalls);
        wsClient.Verify(c => c.Stop(It.IsAny<CancellationTokenSource>(), It.IsAny<bool>()), Times.Once);
        wsClient.VerifyAll();
    }

    [Fact]
    public async Task TranscribeStreamingFeed_NonPcm_Throws()
    {
        var adapter = CreateAdapter(CreateRestClientThatMustNotBeUsed(), CreateListenFactoryThatMustNotBeUsed());

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in adapter.TranscribeStreamingFeed(SingleChunk(new byte[] { 1, 2 }), AudioMimeType.Wav, CancellationToken.None))
            {
            }
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TranscribeStreamingFeed_CancelledConnect_EndsWithoutWaitingForProvider(bool throwCancellation)
    {
        var started = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = new Mock<DeepgramListenWebSocketClient>();
        client.Setup(x => x.Connect(It.IsAny<LiveSchema>(), It.IsAny<CancellationTokenSource>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>()))
            .Callback<LiveSchema, CancellationTokenSource, Dictionary<string, string>, Dictionary<string, string>>((_, source, _, _) =>
            {
                var token = source.Token;
                token.Register(() =>
                {
                    if (throwCancellation) pending.TrySetCanceled(token);
                    else pending.TrySetResult(false);
                });
                started.TrySetResult(token);
            }).Returns(pending.Task);
        bool? cleanupWasCancelled = null;
        client.Setup(x => x.Stop(It.IsAny<CancellationTokenSource>(), false))
            .Callback<CancellationTokenSource, bool>((source, _) => cleanupWasCancelled = source.IsCancellationRequested).ReturnsAsync(true);
        var factory = new DelegatingDeepgramListenWebSocketClientFactory(() => client.Object);
        var adapter = CreateAdapter(CreateRestClientThatMustNotBeUsed(), factory);
        using var caller = new CancellationTokenSource();
        var task = DrainLiveFeedAsync(adapter, caller.Token);
        var sdkToken = await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        caller.Cancel();
        try
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(TimeSpan.FromSeconds(2)));
            Assert.True(sdkToken.IsCancellationRequested);
            Assert.False(cleanupWasCancelled);
            client.Verify(x => x.Send(It.IsAny<byte[]>(), It.IsAny<int>()), Times.Never);
        }
        finally
        {
            pending.TrySetCanceled();
            try { await task.WaitAsync(TimeSpan.FromSeconds(2)); }
            catch (OperationCanceledException) { }
        }
    }

    [Fact]
    public async Task TranscribeStreamingFeed_NormalEnd_KeepsCleanupTokenAliveUntilStopCompletes()
    {
        EventHandler<CloseResponse>? closed = null;
        var stopStarted = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = new Mock<DeepgramListenWebSocketClient>();
        client.Setup(x => x.Subscribe(It.IsAny<EventHandler<CloseResponse>>()))
            .Callback<EventHandler<CloseResponse>>(handler => closed = handler).ReturnsAsync(true);
        client.Setup(x => x.Connect(It.IsAny<LiveSchema>(), It.IsAny<CancellationTokenSource>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>())).ReturnsAsync(true);
        client.Setup(x => x.SendFinalize()).Callback(() => closed!(null, new CloseResponse())).Returns(Task.CompletedTask);
        client.Setup(x => x.Stop(It.IsAny<CancellationTokenSource>(), false))
            .Callback<CancellationTokenSource, bool>((source, _) => stopStarted.TrySetResult(source.Token)).Returns(stopCompleted.Task);
        var adapter = CreateAdapter(CreateRestClientThatMustNotBeUsed(), new DelegatingDeepgramListenWebSocketClientFactory(() => client.Object));
        var task = DrainLiveFeedAsync(adapter, CancellationToken.None);
        var cleanupToken = await stopStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        try
        {
            // Das Testdoppel hat den Ereignisstrom bereits abgeschlossen; die Bereinigung wartet noch auf Stop.
            Assert.False(task.IsCompleted);
            Assert.False(cleanupToken.IsCancellationRequested);
        }
        finally
        {
            stopCompleted.TrySetResult(true);
            await task.WaitAsync(TimeSpan.FromSeconds(2));
        }
    }

    [Theory]
    [InlineData("Synthetic provider failure", false)]
    [InlineData("", false)]
    [InlineData("429 Too Many Requests", true)]
    public async Task TranscribeStreamingFeed_ProviderErrorAndCleanupFailure_PreservesProviderFailure(string message, bool rateLimited)
    {
        EventHandler<ErrorResponse>? error = null;
        var client = new Mock<DeepgramListenWebSocketClient>();
        client.Setup(x => x.Subscribe(It.IsAny<EventHandler<ErrorResponse>>()))
            .Callback<EventHandler<ErrorResponse>>(handler => error = handler).ReturnsAsync(true);
        client.Setup(x => x.Connect(It.IsAny<LiveSchema>(), It.IsAny<CancellationTokenSource>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>())).ReturnsAsync(true);
        client.Setup(x => x.Send(It.IsAny<byte[]>(), It.IsAny<int>()))
            .Callback(() => error!(null, new ErrorResponse { Message = message }));
        client.Setup(x => x.Stop(It.IsAny<CancellationTokenSource>(), false)).ThrowsAsync(new IOException("Synthetic stop failure"));
        client.Setup(x => x.SendClose(false, It.IsAny<CancellationTokenSource>())).ThrowsAsync(new IOException("Synthetic close failure"));
        var adapter = CreateAdapter(CreateRestClientThatMustNotBeUsed(), new DelegatingDeepgramListenWebSocketClientFactory(() => client.Object));

        if (rateLimited)
            await Assert.ThrowsAsync<SpeechProviderRateLimitedException>(() => DrainLiveFeedAsync(adapter, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2)));
        else
        {
            var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => DrainLiveFeedAsync(adapter, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2)));
            Assert.Equal(string.IsNullOrEmpty(message) ? "Deepgram live transcription failed." : message, failure.Message);
        }
        client.Verify(x => x.Stop(It.IsAny<CancellationTokenSource>(), false), Times.Once);
        client.Verify(x => x.SendClose(false, It.IsAny<CancellationTokenSource>()), Times.Once);
    }

    private static async Task DrainLiveFeedAsync(DeepgramSpeechToTextAdapter adapter, CancellationToken token)
    {
        await foreach (var _ in adapter.TranscribeStreamingFeed(SingleChunk(new byte[] { 0, 0 }), AudioMimeType.Pcm, token, "de")) { }
    }

    private static DeepgramSpeechToTextAdapter CreateAdapter(
        DeepgramListenRestClient restClient,
        IDeepgramListenWebSocketClientFactory wsFactory)
        => CreateAdapterWithLogger(restClient, wsFactory).Adapter;

    private static (DeepgramSpeechToTextAdapter Adapter, RecordingLogger<DeepgramPrerecordedTranscriptionClient> Logger) CreateAdapterWithLogger(
        DeepgramListenRestClient restClient,
        IDeepgramListenWebSocketClientFactory wsFactory)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new DeepgramOptions
        {
            ApiKey = "k",
            Endpoint = "https://api.eu.deepgram.com/",
            SttModel = "nova-3-general"
        });
        var logger = new RecordingLogger<DeepgramPrerecordedTranscriptionClient>();
        var prerecordedTranscriptionClient = new DeepgramPrerecordedTranscriptionClient(restClient, options, logger);
        return (new DeepgramSpeechToTextAdapter(wsFactory, prerecordedTranscriptionClient, options, Microsoft.Extensions.Logging.Abstractions.NullLogger<DeepgramSpeechToTextAdapter>.Instance), logger);
    }

    private static void AssertFailedKeytermLog(
        RecordingLogger<DeepgramPrerecordedTranscriptionClient> logger,
        string expectedFailureKind,
        string expectedRequestId,
        string expectedKeyterm)
    {
        var failure = Assert.Single(
            logger.Entries,
            entry => entry.Level == LogLevel.Information
                && entry.Message.Contains("Deepgram prerecorded transcription failed", StringComparison.Ordinal));
        Assert.Equal(expectedFailureKind, failure.Properties["FailureKind"]);
        Assert.Equal(expectedRequestId, failure.Properties["DeepgramRequestId"]);
        Assert.Equal(true, failure.Properties["KeytermParameterPresent"]);
        var loggedKeyterms = JsonSerializer.Deserialize<string[]>(
            Assert.IsType<string>(failure.Properties["KeytermList"]));
        Assert.Contains("ja", loggedKeyterms!);
        Assert.Contains(expectedKeyterm, loggedKeyterms!);
        Assert.Equal(true, failure.Properties["ContextKeytermsRequested"]);
        Assert.Equal(1, failure.Properties["ContextKeytermRequestedCount"]);
        Assert.False(failure.Properties.ContainsKey("Transcript"));
        Assert.False(failure.Properties.ContainsKey("Schema"));
        Assert.False(failure.Properties.ContainsKey("RequestUrl"));
    }

    private static DeepgramListenRestClient CreateRestClientThatMustNotBeUsed()
    {
        var restClient = new Mock<DeepgramListenRestClient>(MockBehavior.Strict);
        return restClient.Object;
    }

    private static IDeepgramListenWebSocketClientFactory CreateListenFactoryThatMustNotBeUsed()
    {
        return new DelegatingDeepgramListenWebSocketClientFactory(static () =>
            throw new InvalidOperationException("WebSocket client factory should not be used in this scenario."));
    }

    private static async IAsyncEnumerable<byte[]> SingleChunk(byte[] chunk)
    {
        await Task.Yield();
        yield return chunk;
    }

    private static byte[] PcmForDurationMs(int durationMs)
        => new byte[16_000 * 2 * durationMs / 1_000];

    private static IAsyncEnumerable<byte[]> SingleChunkThenWait(byte[] chunk) =>
        SingleChunkThenWaitCore(chunk, CancellationToken.None);

    private static async IAsyncEnumerable<byte[]> SingleChunkThenWaitCore(byte[] chunk, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return chunk;

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private static ResultResponse CreateResult(string transcript, bool isFinal, double confidence = 0.0)
    {
        return new ResultResponse
        {
            Channel = new Channel
            {
                Alternatives = new[] { new Alternative { Transcript = transcript, Confidence = confidence } }
            },
            IsFinal = isFinal
        };
    }
}
