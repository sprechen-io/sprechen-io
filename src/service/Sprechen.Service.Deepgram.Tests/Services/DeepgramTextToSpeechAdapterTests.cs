using global::Deepgram.Models.Speak.v2.WebSocket;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Sprechen.Service.Deepgram.Services;
using System.Net.WebSockets;
using Xunit;
using DeepgramSpeakWebSocketClient = global::Deepgram.Clients.Interfaces.v2.ISpeakWebSocketClient;

using Sprechen.Service.Deepgram.Options;

namespace Sprechen.Service.Deepgram.Tests.Services;

public sealed class DeepgramTextToSpeechAdapterTests
{
    [Fact]
    public async Task SynthesizeAsync_Pcm_UsesWebSocketAndStreamsAudio()
    {
        EventHandler<AudioResponse>? onAudio = null;
        EventHandler<FlushedResponse>? onFlushed = null;
        SpeakSchema? capturedSchema = null;
        Dictionary<string, string>? capturedAddons = null;

        var wsClient = new Mock<DeepgramSpeakWebSocketClient>(MockBehavior.Strict);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<AudioResponse>>()))
            .Callback<EventHandler<AudioResponse>>(handler => onAudio = handler)
            .ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<WarningResponse>>()))
            .ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<ErrorResponse>>()))
            .ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<FlushedResponse>>()))
            .Callback<EventHandler<FlushedResponse>>(handler => onFlushed = handler)
            .ReturnsAsync(true);

        wsClient.Setup(c => c.Connect(
                It.IsAny<SpeakSchema>(),
                It.IsAny<CancellationTokenSource>(),
                It.Is<Dictionary<string, string>>(addons => addons.ContainsKey("mip_opt_out") && addons["mip_opt_out"] == "true"),
                It.IsAny<Dictionary<string, string>>()))
            .Callback<SpeakSchema, CancellationTokenSource, Dictionary<string, string>, Dictionary<string, string>>((schema, _, addons, _) =>
            {
                capturedSchema = schema;
                capturedAddons = addons;
            })
            .ReturnsAsync(true);

        wsClient.Setup(c => c.SpeakWithText(It.IsAny<string>()))
            .Callback(() =>
            {
                onAudio?.Invoke(null, new AudioResponse { Stream = new MemoryStream(new byte[] { 1, 2, 3, 4 }) });
            });

        wsClient.Setup(c => c.Flush())
            .Callback(() => onFlushed?.Invoke(null, new FlushedResponse()));

        // Nach Flushed wird die Verbindung mit SendClose geschlossen und der Client freigegeben.
        wsClient.Setup(c => c.SendClose(It.IsAny<bool>(), It.IsAny<CancellationTokenSource>())).Returns(Task.CompletedTask);
        wsClient.As<IDisposable>().Setup(c => c.Dispose());

        var factory = new DelegatingDeepgramSpeakWebSocketClientFactory(() => wsClient.Object);

        var options = Microsoft.Extensions.Options.Options.Create(new DeepgramOptions
        {
            ApiKey = "k",
            Endpoint = "https://api.eu.deepgram.com/",
            TtsModels = new Dictionary<string, string> { ["de"] = "aura-2-aurelia-de" }
        });

        var modelMap = new DeepgramTtsModelMap(options);

        var adapter = new DeepgramTextToSpeechAdapter(
            factory,
            modelMap,
            options,
        NullLogger<DeepgramTextToSpeechAdapter>.Instance);

        var chunks = new List<byte[]>();
        await foreach (var audioChunk in adapter.SynthesizeAsync("Hello", AudioMimeType.Pcm, "de", CancellationToken.None))
        {
            Assert.Equal(AudioMimeType.Pcm, audioChunk.Mime);
            Assert.Equal(24_000, audioChunk.SampleRateHz);
            chunks.Add(audioChunk.Data);
        }

        var chunk = Assert.Single(chunks);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, chunk);

        Assert.NotNull(capturedSchema);
        Assert.Equal("aura-2-aurelia-de", capturedSchema!.Model);
        Assert.Equal("linear16", capturedSchema.Encoding);
        Assert.Equal(24_000, capturedSchema.SampleRate);

        Assert.NotNull(capturedAddons);
        Assert.Equal("true", capturedAddons!["mip_opt_out"]);
        Assert.Equal(1, factory.CreateCalls);

        // Die Bereinigung darf hier kein zusätzliches Stop ausführen.
        wsClient.Verify(c => c.Stop(It.IsAny<CancellationTokenSource>(), It.IsAny<bool>()), Times.Never);
        wsClient.Verify(c => c.SendClose(It.IsAny<bool>(), It.IsAny<CancellationTokenSource>()), Times.Once);
        wsClient.As<IDisposable>().Verify(c => c.Dispose(), Times.Once);

        wsClient.VerifyAll();
    }

    [Fact]
    public async Task SynthesizeAsync_NonPcm_Throws()
    {
        var factory = CreateSpeakFactoryThatMustNotBeUsed();
        var options = Microsoft.Extensions.Options.Options.Create(new DeepgramOptions { ApiKey = "k", Endpoint = "https://api.eu.deepgram.com/" });
        var modelMap = new DeepgramTtsModelMap(options);

        var adapter = new DeepgramTextToSpeechAdapter(
            factory,
            modelMap,
            options,
            NullLogger<DeepgramTextToSpeechAdapter>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in adapter.SynthesizeAsync("Hello", AudioMimeType.Mp3, "de", CancellationToken.None))
            {
            }
        });
    }

    [Fact]
    public async Task SynthesizeAsync_MissingModelMapping_Throws()
    {
        var factory = CreateSpeakFactoryThatMustNotBeUsed();
        var options = Microsoft.Extensions.Options.Options.Create(new DeepgramOptions
        {
            ApiKey = "k",
            Endpoint = "https://api.eu.deepgram.com/",
            TtsModels = new Dictionary<string, string> { ["en"] = "aura-2-thalia-en" }
        });
        var modelMap = new DeepgramTtsModelMap(options);

        var adapter = new DeepgramTextToSpeechAdapter(
            factory,
            modelMap,
            options,
            NullLogger<DeepgramTextToSpeechAdapter>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in adapter.SynthesizeAsync("Hello", AudioMimeType.Pcm, "de", CancellationToken.None))
            {
            }
        });
    }

    [Fact]
    public async Task SynthesizeAsync_MissingLanguageCode_Throws()
    {
        var factory = CreateSpeakFactoryThatMustNotBeUsed();
        var options = Microsoft.Extensions.Options.Options.Create(new DeepgramOptions
        {
            ApiKey = "k",
            Endpoint = "https://api.eu.deepgram.com/",
            TtsModels = new Dictionary<string, string> { ["de"] = "aura-2-aurelia-de" }
        });
        var modelMap = new DeepgramTtsModelMap(options);

        var adapter = new DeepgramTextToSpeechAdapter(
            factory,
            modelMap,
            options,
            NullLogger<DeepgramTextToSpeechAdapter>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in adapter.SynthesizeAsync("Hello", AudioMimeType.Pcm, languageCode: null, CancellationToken.None))
            {
            }
        });
    }

    [Fact]
    public async Task SynthesizeAsync_InvalidSampleRate_Throws()
    {
        var factory = CreateSpeakFactoryThatMustNotBeUsed();
        var options = Microsoft.Extensions.Options.Options.Create(new DeepgramOptions
        {
            ApiKey = "k",
            Endpoint = "https://api.eu.deepgram.com/",
            TtsSampleRateHz = 12_345
        });
        var modelMap = new DeepgramTtsModelMap(options);

        var adapter = new DeepgramTextToSpeechAdapter(
            factory,
            modelMap,
            options,
            NullLogger<DeepgramTextToSpeechAdapter>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in adapter.SynthesizeAsync("Hello", AudioMimeType.Pcm, "de", CancellationToken.None))
            {
            }
        });
    }

    [Fact]
    public async Task SynthesizeAsync_WhenConnectReturnsFalse_Throws()
    {
        var wsClient = new Mock<DeepgramSpeakWebSocketClient>(MockBehavior.Strict);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<AudioResponse>>())).ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<WarningResponse>>())).ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<ErrorResponse>>())).ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<FlushedResponse>>())).ReturnsAsync(true);
        wsClient.Setup(c => c.Connect(
                It.IsAny<SpeakSchema>(),
                It.IsAny<CancellationTokenSource>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(false);
        wsClient.As<IDisposable>().Setup(c => c.Dispose());

        var factory = new DelegatingDeepgramSpeakWebSocketClientFactory(() => wsClient.Object);

        var options = Microsoft.Extensions.Options.Options.Create(new DeepgramOptions
        {
            ApiKey = "k",
            Endpoint = "https://api.eu.deepgram.com/",
            TtsRateLimitMaxRetries = 2,
            TtsRateLimitInitialDelayMs = 0,
            TtsRateLimitMaxDelayMs = 0
        });
        var modelMap = new DeepgramTtsModelMap(options);

        var adapter = new DeepgramTextToSpeechAdapter(
            factory,
            modelMap,
            options,
            NullLogger<DeepgramTextToSpeechAdapter>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in adapter.SynthesizeAsync("Hello", AudioMimeType.Pcm, "de", CancellationToken.None))
            {
            }
        });

        wsClient.As<IDisposable>().Verify(c => c.Dispose(), Times.Once);
        Assert.Equal(1, factory.CreateCalls);

        wsClient.VerifyAll();
    }

    [Fact]
    public async Task SynthesizeAsync_WhenConnectThrowsWebSocketException_Wraps()
    {
        var wsClient = new Mock<DeepgramSpeakWebSocketClient>(MockBehavior.Strict);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<AudioResponse>>())).ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<WarningResponse>>())).ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<ErrorResponse>>())).ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<FlushedResponse>>())).ReturnsAsync(true);
        wsClient.Setup(c => c.Connect(
                It.IsAny<SpeakSchema>(),
                It.IsAny<CancellationTokenSource>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<Dictionary<string, string>>()))
            .ThrowsAsync(new WebSocketException("boom"));
        wsClient.As<IDisposable>().Setup(c => c.Dispose());

        var factory = new DelegatingDeepgramSpeakWebSocketClientFactory(() => wsClient.Object);

        var options = Microsoft.Extensions.Options.Options.Create(new DeepgramOptions
        {
            ApiKey = "k",
            Endpoint = "https://api.eu.deepgram.com/",
            TtsEndpoint = "https://api.deepgram.com"
        });
        var modelMap = new DeepgramTtsModelMap(options);

        var adapter = new DeepgramTextToSpeechAdapter(
            factory,
            modelMap,
            options,
            NullLogger<DeepgramTextToSpeechAdapter>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in adapter.SynthesizeAsync("Hello", AudioMimeType.Pcm, "de", CancellationToken.None))
            {
            }
        });

        Assert.Contains("connection failed", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("wss://", exception.Message, StringComparison.OrdinalIgnoreCase);

        wsClient.As<IDisposable>().Verify(c => c.Dispose(), Times.Once);
        Assert.Equal(1, factory.CreateCalls);

        wsClient.VerifyAll();
    }

    [Fact]
    public async Task SynthesizeAsync_WhenConnectReturns429_ThrowsRateLimited()
    {
        var clients = Enumerable.Range(0, 3).Select(_ => CreateRateLimitedClient()).ToArray();
        var next = 0;
        var factory = new DelegatingDeepgramSpeakWebSocketClientFactory(() => clients[next++].Object);
        var options = Microsoft.Extensions.Options.Options.Create(new DeepgramOptions
        {
            ApiKey = "synthetic",
            TtsRateLimitMaxRetries = 2,
            TtsRateLimitInitialDelayMs = 0,
            TtsRateLimitMaxDelayMs = 0
        });
        var adapter = new DeepgramTextToSpeechAdapter(factory, new DeepgramTtsModelMap(options), options, NullLogger<DeepgramTextToSpeechAdapter>.Instance);
        using var caller = new CancellationTokenSource();
        var synthesis = ReadSynthesisAsync(adapter, caller.Token);
        try
        {
            var exception = await Assert.ThrowsAsync<SpeechProviderRateLimitedException>(() => synthesis.WaitAsync(TimeSpan.FromSeconds(2)));
            Assert.Equal("Deepgram", exception.Provider);
            Assert.Equal("TextToSpeech", exception.Operation);
            Assert.Equal(3, factory.CreateCalls);
            foreach (var client in clients)
            {
                client.Verify(x => x.Connect(It.IsAny<SpeakSchema>(), It.IsAny<CancellationTokenSource>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>()), Times.Once);
                client.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
                client.Verify(x => x.SpeakWithText(It.IsAny<string>()), Times.Never);
            }
        }
        finally
        {
            caller.Cancel();
            try { await synthesis.WaitAsync(TimeSpan.FromSeconds(2)); }
            catch (OperationCanceledException) { }
            catch (SpeechProviderRateLimitedException) { }
        }
    }

    [Fact]
    public async Task SynthesizeAsync_WhenConnectReturns429WithDoubleQuotes_ThrowsRateLimited()
    {
        var wsClient = new Mock<DeepgramSpeakWebSocketClient>(MockBehavior.Strict);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<AudioResponse>>())).ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<WarningResponse>>())).ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<ErrorResponse>>())).ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<FlushedResponse>>())).ReturnsAsync(true);
        wsClient.Setup(c => c.Connect(
                It.IsAny<SpeakSchema>(),
                It.IsAny<CancellationTokenSource>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<Dictionary<string, string>>()))
            .ThrowsAsync(new WebSocketException("The server returned status code \"429\" when status code \"101\" was expected."));
        wsClient.As<IDisposable>().Setup(c => c.Dispose());

        var factory = new DelegatingDeepgramSpeakWebSocketClientFactory(() => wsClient.Object);

        var options = Microsoft.Extensions.Options.Options.Create(new DeepgramOptions
        {
            ApiKey = "k",
            Endpoint = "https://api.eu.deepgram.com/",
            TtsRateLimitMaxRetries = 0
        });
        var modelMap = new DeepgramTtsModelMap(options);

        var adapter = new DeepgramTextToSpeechAdapter(
            factory,
            modelMap,
            options,
            NullLogger<DeepgramTextToSpeechAdapter>.Instance);

        await Assert.ThrowsAsync<SpeechProviderRateLimitedException>(async () =>
        {
            await foreach (var _ in adapter.SynthesizeAsync("Hello", AudioMimeType.Pcm, "de", CancellationToken.None))
            {
            }
        });

        wsClient.Verify(c => c.Connect(
                It.IsAny<SpeakSchema>(),
                It.IsAny<CancellationTokenSource>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<Dictionary<string, string>>()),
            Times.Once);

        wsClient.As<IDisposable>().Verify(c => c.Dispose(), Times.Once);
        Assert.Equal(1, factory.CreateCalls);

        wsClient.VerifyAll();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SynthesizeAsync_WhenConsumerStopsEarly_StopsAndClosesWebSocket(bool cancelCaller)
    {
        EventHandler<AudioResponse>? onAudio = null;
        using var caller = new CancellationTokenSource();
        bool? cleanupWasCancelled = null;

        var wsClient = new Mock<DeepgramSpeakWebSocketClient>(MockBehavior.Strict);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<AudioResponse>>()))
            .Callback<EventHandler<AudioResponse>>(handler => onAudio = handler)
            .ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<WarningResponse>>()))
            .ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<ErrorResponse>>()))
            .ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<FlushedResponse>>()))
            .ReturnsAsync(true);

        wsClient.Setup(c => c.Connect(
                It.IsAny<SpeakSchema>(),
                It.IsAny<CancellationTokenSource>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(true);

        wsClient.Setup(c => c.SpeakWithText(It.IsAny<string>()))
            .Callback(() =>
            {
                onAudio?.Invoke(null, new AudioResponse { Stream = new MemoryStream(new byte[] { 1, 2, 3 }) });
            });

        wsClient.Setup(c => c.Flush());
        wsClient.Setup(c => c.Stop(It.IsAny<CancellationTokenSource>(), It.IsAny<bool>()))
            .Callback<CancellationTokenSource, bool>((source, _) => cleanupWasCancelled = source.IsCancellationRequested)
            .ReturnsAsync(true);
        wsClient.As<IDisposable>().Setup(c => c.Dispose());

        var factory = new DelegatingDeepgramSpeakWebSocketClientFactory(() => wsClient.Object);

        var options = Microsoft.Extensions.Options.Options.Create(new DeepgramOptions
        {
            ApiKey = "k",
            Endpoint = "https://api.eu.deepgram.com/"
        });
        var modelMap = new DeepgramTtsModelMap(options);

        var adapter = new DeepgramTextToSpeechAdapter(
            factory,
            modelMap,
            options,
            NullLogger<DeepgramTextToSpeechAdapter>.Instance);

        var received = new List<AudioSynthesisChunk>();
        await foreach (var chunk in adapter.SynthesizeAsync("Hello", AudioMimeType.Pcm, "de", caller.Token))
        {
            received.Add(chunk);
            if (cancelCaller) caller.Cancel();
            break;
        }

        Assert.Single(received);
        Assert.False(cleanupWasCancelled);

        wsClient.Verify(c => c.Stop(It.IsAny<CancellationTokenSource>(), It.IsAny<bool>()), Times.Once);
        wsClient.As<IDisposable>().Verify(c => c.Dispose(), Times.Once);
        Assert.Equal(1, factory.CreateCalls);

        wsClient.VerifyAll();
    }

    [Fact]
    public async Task SynthesizeAsync_WhenStopThrowsNre_UsesSendCloseAndClosesWebSocket()
    {
        EventHandler<AudioResponse>? onAudio = null;

        var wsClient = new Mock<DeepgramSpeakWebSocketClient>(MockBehavior.Strict);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<AudioResponse>>()))
            .Callback<EventHandler<AudioResponse>>(handler => onAudio = handler)
            .ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<WarningResponse>>()))
            .ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<ErrorResponse>>()))
            .ReturnsAsync(true);
        wsClient.Setup(c => c.Subscribe(It.IsAny<EventHandler<FlushedResponse>>()))
            .ReturnsAsync(true);

        wsClient.Setup(c => c.Connect(
                It.IsAny<SpeakSchema>(),
                It.IsAny<CancellationTokenSource>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(true);

        wsClient.Setup(c => c.SpeakWithText(It.IsAny<string>()))
            .Callback(() =>
            {
                onAudio?.Invoke(null, new AudioResponse { Stream = new MemoryStream(new byte[] { 1, 2, 3 }) });
            });

        wsClient.Setup(c => c.Flush());
        wsClient.Setup(c => c.Stop(It.IsAny<CancellationTokenSource>(), It.IsAny<bool>()))
            .Throws(new NullReferenceException("Deepgram stop bug"));
        wsClient.Setup(c => c.SendClose(It.IsAny<bool>(), It.IsAny<CancellationTokenSource>())).Returns(Task.CompletedTask);
        wsClient.As<IDisposable>().Setup(c => c.Dispose());

        var factory = new DelegatingDeepgramSpeakWebSocketClientFactory(() => wsClient.Object);

        var options = Microsoft.Extensions.Options.Options.Create(new DeepgramOptions
        {
            ApiKey = "k",
            Endpoint = "https://api.eu.deepgram.com/"
        });
        var modelMap = new DeepgramTtsModelMap(options);

        var adapter = new DeepgramTextToSpeechAdapter(
            factory,
            modelMap,
            options,
            NullLogger<DeepgramTextToSpeechAdapter>.Instance);

        var received = new List<AudioSynthesisChunk>();
        await foreach (var chunk in adapter.SynthesizeAsync("Hello", AudioMimeType.Pcm, "de", CancellationToken.None))
        {
            received.Add(chunk);
            break;
        }

        Assert.Single(received);

        wsClient.Verify(c => c.Stop(It.IsAny<CancellationTokenSource>(), It.IsAny<bool>()), Times.Once);
        wsClient.Verify(c => c.SendClose(It.IsAny<bool>(), It.IsAny<CancellationTokenSource>()), Times.Once);
        wsClient.As<IDisposable>().Verify(c => c.Dispose(), Times.Once);
        Assert.Equal(1, factory.CreateCalls);

        wsClient.VerifyAll();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SynthesizeAsync_CancelledConnect_ReleasesSlotForSuccessfulNextClient(bool throwCancellation)
    {
        var started = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstClient = new Mock<DeepgramSpeakWebSocketClient>();
        firstClient.As<IDisposable>().Setup(x => x.Dispose());
        firstClient.Setup(x => x.Connect(It.IsAny<SpeakSchema>(), It.IsAny<CancellationTokenSource>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>()))
            .Callback<SpeakSchema, CancellationTokenSource, Dictionary<string, string>, Dictionary<string, string>>((_, source, _, _) =>
            {
                var token = source.Token;
                token.Register(() =>
                {
                    if (throwCancellation) pending.TrySetCanceled(token);
                    else pending.TrySetResult(false);
                });
                started.TrySetResult(token);
            }).Returns(pending.Task);

        EventHandler<AudioResponse>? audio = null;
        EventHandler<FlushedResponse>? flushed = null;
        var nextClient = new Mock<DeepgramSpeakWebSocketClient>();
        nextClient.As<IDisposable>().Setup(x => x.Dispose());
        nextClient.Setup(x => x.Subscribe(It.IsAny<EventHandler<AudioResponse>>()))
            .Callback<EventHandler<AudioResponse>>(handler => audio = handler).ReturnsAsync(true);
        nextClient.Setup(x => x.Subscribe(It.IsAny<EventHandler<FlushedResponse>>()))
            .Callback<EventHandler<FlushedResponse>>(handler => flushed = handler).ReturnsAsync(true);
        nextClient.Setup(x => x.Connect(It.IsAny<SpeakSchema>(), It.IsAny<CancellationTokenSource>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>())).ReturnsAsync(true);
        nextClient.Setup(x => x.SpeakWithText(It.IsAny<string>()))
            .Callback(() => audio!(null, new AudioResponse { Stream = new MemoryStream(new byte[] { 4, 2 }) }));
        nextClient.Setup(x => x.Flush()).Callback(() => flushed!(null, new FlushedResponse()));
        var clients = new Queue<DeepgramSpeakWebSocketClient>(new[] { firstClient.Object, nextClient.Object });
        var factory = new DelegatingDeepgramSpeakWebSocketClientFactory(() => clients.Dequeue());
        var options = Microsoft.Extensions.Options.Options.Create(new DeepgramOptions { ApiKey = "synthetic", TtsMaxConcurrency = 1 });
        var adapter = new DeepgramTextToSpeechAdapter(factory, new DeepgramTtsModelMap(options), options, NullLogger<DeepgramTextToSpeechAdapter>.Instance);
        using var caller = new CancellationTokenSource();
        using var waitingCaller = new CancellationTokenSource();
        var first = ReadSynthesisAsync(adapter, caller.Token);
        var sdkToken = await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        try
        {
            var waiting = ReadSynthesisAsync(adapter, waitingCaller.Token);
            Assert.Equal(1, factory.CreateCalls);
            waitingCaller.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting.WaitAsync(TimeSpan.FromSeconds(2)));
            Assert.Equal(1, factory.CreateCalls);
            caller.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first.WaitAsync(TimeSpan.FromSeconds(2)));
            Assert.True(sdkToken.IsCancellationRequested);
            var received = await ReadSynthesisAsync(adapter, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));
            Assert.Equal(new byte[] { 4, 2 }, Assert.Single(received).Data);
            Assert.Equal(2, factory.CreateCalls);
            firstClient.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
            nextClient.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
        }
        finally
        {
            pending.TrySetCanceled();
            try { await first.WaitAsync(TimeSpan.FromSeconds(2)); }
            catch (OperationCanceledException) { }
        }
    }

    [Fact]
    public async Task SynthesizeAsync_CancelledBeforeStart_DoesNotCreateClient()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new DeepgramOptions { ApiKey = "synthetic" });
        var adapter = new DeepgramTextToSpeechAdapter(CreateSpeakFactoryThatMustNotBeUsed(), new DeepgramTtsModelMap(options), options, NullLogger<DeepgramTextToSpeechAdapter>.Instance);
        using var caller = new CancellationTokenSource();
        caller.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ReadSynthesisAsync(adapter, caller.Token));
    }

    [Fact]
    public async Task CleanupAsync_PendingSendClose_CancelsPendingWorkAndDisposesWithoutSynchronousClose()
    {
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var source = new CancellationTokenSource();
        using var registration = source.Token.Register(() => pending.TrySetCanceled(source.Token));
        var client = new Mock<DeepgramSpeakWebSocketClient>();
        client.Setup(x => x.SendClose(false, source)).Returns(pending.Task);
        client.As<IDisposable>().Setup(x => x.Dispose());
        try
        {
            await new DeepgramSpeakConnectionCleanup(NullLogger.Instance)
                .CleanupAsync(client.Object, source, true, true, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(4));
            Assert.True(source.IsCancellationRequested);
            Assert.True(pending.Task.IsCanceled);
            client.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
            client.Verify(x => x.Close(It.IsAny<bool>()), Times.Never);
        }
        finally
        {
            source.Cancel();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CleanupAsync_ProtocolAndDisposalFail_DoesNotMaskTheOriginalStreamOutcome(bool callerCancelled)
    {
        using var caller = new CancellationTokenSource();
        using var source = new CancellationTokenSource();
        if (callerCancelled) caller.Cancel();
        var client = new Mock<DeepgramSpeakWebSocketClient>();
        client.Setup(x => x.Stop(source, false)).ThrowsAsync(new TimeoutException("Synthetic stop timeout"));
        client.Setup(x => x.SendClose(false, source))
            .ThrowsAsync(callerCancelled ? new OperationCanceledException(caller.Token) : new IOException("Synthetic close failure"));
        client.As<IDisposable>().Setup(x => x.Dispose()).Throws(new InvalidOperationException("Synthetic disposal failure"));

        await new DeepgramSpeakConnectionCleanup(NullLogger.Instance)
            .CleanupAsync(client.Object, source, true, false, caller.Token);

        Assert.True(source.IsCancellationRequested);
        client.Verify(x => x.Stop(source, false), Times.Once);
        client.Verify(x => x.SendClose(false, source), Times.Once);
        client.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
        client.Verify(x => x.Close(It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task SynthesizeAsync_CancelledWhileWaitingForAudio_StopsWithIndependentCleanupToken()
    {
        var waitingForAudio = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool? cleanupWasCancelled = null;
        var client = new Mock<DeepgramSpeakWebSocketClient>();
        client.As<IDisposable>().Setup(x => x.Dispose());
        client.Setup(x => x.Connect(It.IsAny<SpeakSchema>(), It.IsAny<CancellationTokenSource>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>())).ReturnsAsync(true);
        client.Setup(x => x.Flush()).Callback(() => waitingForAudio.TrySetResult());
        client.Setup(x => x.Stop(It.IsAny<CancellationTokenSource>(), false))
            .Callback<CancellationTokenSource, bool>((source, _) => cleanupWasCancelled = source.IsCancellationRequested).ReturnsAsync(true);
        var options = Microsoft.Extensions.Options.Options.Create(new DeepgramOptions { ApiKey = "synthetic" });
        var adapter = new DeepgramTextToSpeechAdapter(new DelegatingDeepgramSpeakWebSocketClientFactory(() => client.Object), new DeepgramTtsModelMap(options), options, NullLogger<DeepgramTextToSpeechAdapter>.Instance);
        using var caller = new CancellationTokenSource();
        var synthesis = ReadSynthesisAsync(adapter, caller.Token);
        try
        {
            await waitingForAudio.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.False(synthesis.IsCompleted);
            caller.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => synthesis.WaitAsync(TimeSpan.FromSeconds(2)));
            Assert.False(cleanupWasCancelled);
            client.Verify(x => x.Stop(It.IsAny<CancellationTokenSource>(), false), Times.Once);
            client.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
        }
        finally
        {
            caller.Cancel();
            try { await synthesis.WaitAsync(TimeSpan.FromSeconds(2)); }
            catch (OperationCanceledException) { }
        }
    }

    [Fact]
    public async Task SynthesizeAsync_RateLimitedThenConnected_ReceivesAudioFromFreshClient()
    {
        var failed = CreateRateLimitedClient();
        bool? cleanupWasCancelled = null;
        EventHandler<AudioResponse>? audio = null;
        EventHandler<FlushedResponse>? flushed = null;
        var fresh = new Mock<DeepgramSpeakWebSocketClient>(MockBehavior.Strict);
        fresh.Setup(x => x.Subscribe(It.IsAny<EventHandler<AudioResponse>>())).Callback<EventHandler<AudioResponse>>(handler => audio = handler).ReturnsAsync(true);
        fresh.Setup(x => x.Subscribe(It.IsAny<EventHandler<FlushedResponse>>())).Callback<EventHandler<FlushedResponse>>(handler => flushed = handler).ReturnsAsync(true);
        fresh.Setup(x => x.Subscribe(It.IsAny<EventHandler<WarningResponse>>())).ReturnsAsync(true);
        fresh.Setup(x => x.Subscribe(It.IsAny<EventHandler<ErrorResponse>>())).ReturnsAsync(true);
        fresh.Setup(x => x.Connect(It.IsAny<SpeakSchema>(), It.IsAny<CancellationTokenSource>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>())).ReturnsAsync(true);
        fresh.Setup(x => x.SpeakWithText(It.IsAny<string>())).Callback(() => audio!(null, new AudioResponse { Stream = new MemoryStream(new byte[] { 8, 4 }) }));
        fresh.Setup(x => x.Flush()).Callback(() => flushed!(null, new FlushedResponse()));
        fresh.Setup(x => x.SendClose(false, It.IsAny<CancellationTokenSource>()))
            .Callback<bool, CancellationTokenSource>((_, source) => cleanupWasCancelled = source.IsCancellationRequested)
            .Returns(Task.CompletedTask);
        fresh.As<IDisposable>().Setup(x => x.Dispose());
        var clients = new Queue<DeepgramSpeakWebSocketClient>(new[] { failed.Object, fresh.Object });
        var factory = new DelegatingDeepgramSpeakWebSocketClientFactory(() => clients.Dequeue());
        var options = Microsoft.Extensions.Options.Options.Create(new DeepgramOptions { ApiKey = "synthetic", TtsRateLimitInitialDelayMs = 0, TtsRateLimitMaxDelayMs = 0 });
        var adapter = new DeepgramTextToSpeechAdapter(factory, new DeepgramTtsModelMap(options), options, NullLogger<DeepgramTextToSpeechAdapter>.Instance);
        using var caller = new CancellationTokenSource();
        var synthesis = ReadSynthesisAsync(adapter, caller.Token);
        try
        {
            Assert.Equal(new byte[] { 8, 4 }, Assert.Single(await synthesis.WaitAsync(TimeSpan.FromSeconds(2))).Data);
            Assert.Equal(2, factory.CreateCalls);
            Assert.False(cleanupWasCancelled);
            failed.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
            failed.Verify(x => x.Connect(It.IsAny<SpeakSchema>(), It.IsAny<CancellationTokenSource>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>()), Times.Once);
            fresh.VerifyAll();
        }
        finally
        {
            caller.Cancel();
            try { await synthesis.WaitAsync(TimeSpan.FromSeconds(2)); }
            catch (OperationCanceledException) { }
        }
    }

    [Fact]
    public async Task SynthesizeAsync_CancelledDuringBackoff_DisposesFailedClientWithoutAnotherAttempt()
    {
        var disposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failed = CreateRateLimitedClient();
        failed.As<IDisposable>().Setup(x => x.Dispose()).Callback(() => disposed.TrySetResult());
        var factory = new DelegatingDeepgramSpeakWebSocketClientFactory(() => failed.Object);
        var options = Microsoft.Extensions.Options.Options.Create(new DeepgramOptions { ApiKey = "synthetic", TtsRateLimitInitialDelayMs = 30000, TtsRateLimitMaxDelayMs = 30000 });
        var adapter = new DeepgramTextToSpeechAdapter(factory, new DeepgramTtsModelMap(options), options, NullLogger<DeepgramTextToSpeechAdapter>.Instance);
        using var caller = new CancellationTokenSource();
        var synthesis = ReadSynthesisAsync(adapter, caller.Token);
        try
        {
            await disposed.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.False(synthesis.IsCompleted);
            caller.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => synthesis.WaitAsync(TimeSpan.FromSeconds(2)));
            Assert.Equal(1, factory.CreateCalls);
            failed.Verify(x => x.Connect(It.IsAny<SpeakSchema>(), It.IsAny<CancellationTokenSource>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>()), Times.Once);
            failed.As<IDisposable>().Verify(x => x.Dispose(), Times.Once);
        }
        finally
        {
            caller.Cancel();
            try { await synthesis.WaitAsync(TimeSpan.FromSeconds(2)); }
            catch (OperationCanceledException) { }
        }
    }

    [Fact]
    public async Task SynthesizeAsync_CancelledAtSuccessfulConnect_StopsBeforeReleasingClient()
    {
        using var caller = new CancellationTokenSource();
        bool? cleanupWasCancelled = null;
        var client = new Mock<DeepgramSpeakWebSocketClient>(MockBehavior.Strict);
        client.Setup(x => x.Subscribe(It.IsAny<EventHandler<AudioResponse>>())).ReturnsAsync(true);
        client.Setup(x => x.Subscribe(It.IsAny<EventHandler<FlushedResponse>>())).ReturnsAsync(true);
        client.Setup(x => x.Subscribe(It.IsAny<EventHandler<WarningResponse>>())).ReturnsAsync(true);
        client.Setup(x => x.Subscribe(It.IsAny<EventHandler<ErrorResponse>>())).ReturnsAsync(true);
        client.Setup(x => x.Connect(It.IsAny<SpeakSchema>(), It.IsAny<CancellationTokenSource>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>())).Callback(caller.Cancel).ReturnsAsync(true);
        client.Setup(x => x.Stop(It.IsAny<CancellationTokenSource>(), false)).Callback<CancellationTokenSource, bool>((source, _) => cleanupWasCancelled = source.IsCancellationRequested).ReturnsAsync(true);
        client.As<IDisposable>().Setup(x => x.Dispose());
        var options = Microsoft.Extensions.Options.Options.Create(new DeepgramOptions { ApiKey = "synthetic" });
        var adapter = new DeepgramTextToSpeechAdapter(new DelegatingDeepgramSpeakWebSocketClientFactory(() => client.Object), new DeepgramTtsModelMap(options), options, NullLogger<DeepgramTextToSpeechAdapter>.Instance);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ReadSynthesisAsync(adapter, caller.Token));
        Assert.False(cleanupWasCancelled);
        client.VerifyAll();
    }

    private static Mock<DeepgramSpeakWebSocketClient> CreateRateLimitedClient()
    {
        var client = new Mock<DeepgramSpeakWebSocketClient>(MockBehavior.Strict);
        client.Setup(x => x.Subscribe(It.IsAny<EventHandler<AudioResponse>>())).ReturnsAsync(true);
        client.Setup(x => x.Subscribe(It.IsAny<EventHandler<FlushedResponse>>())).ReturnsAsync(true);
        client.Setup(x => x.Subscribe(It.IsAny<EventHandler<WarningResponse>>())).ReturnsAsync(true);
        client.Setup(x => x.Subscribe(It.IsAny<EventHandler<ErrorResponse>>())).ReturnsAsync(true);
        client.SetupSequence(x => x.Connect(It.IsAny<SpeakSchema>(), It.IsAny<CancellationTokenSource>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<Dictionary<string, string>>()))
            .ThrowsAsync(new WebSocketException("The server returned status code '429' when status code '101' was expected."))
            .ReturnsAsync(true);
        client.Setup(x => x.SpeakWithText(It.IsAny<string>()));
        client.Setup(x => x.Flush());
        client.Setup(x => x.Stop(It.IsAny<CancellationTokenSource>(), false)).ReturnsAsync(true);
        client.Setup(x => x.SendClose(false, It.IsAny<CancellationTokenSource>())).Returns(Task.CompletedTask);
        client.As<IDisposable>().Setup(x => x.Dispose());
        return client;
    }

    private static async Task<List<AudioSynthesisChunk>> ReadSynthesisAsync(DeepgramTextToSpeechAdapter adapter, CancellationToken token)
    {
        var result = new List<AudioSynthesisChunk>();
        await foreach (var chunk in adapter.SynthesizeAsync("Synthetic prompt", AudioMimeType.Pcm, "de", token)) result.Add(chunk);
        return result;
    }

    private static IDeepgramSpeakWebSocketClientFactory CreateSpeakFactoryThatMustNotBeUsed()
    {
        return new DelegatingDeepgramSpeakWebSocketClientFactory(static () =>
            throw new InvalidOperationException("WebSocket client factory should not be used in this scenario."));
    }
}
