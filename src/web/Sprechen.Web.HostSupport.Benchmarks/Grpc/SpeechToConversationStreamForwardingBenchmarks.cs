using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Sprechen.Proto;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sprechen.Web.HostSupport.Extensions;
using SpeechGrpcClient = Sprechen.Proto.Speech.SpeechClient;

namespace Sprechen.Web.HostSupport.Benchmarks.Grpc;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0, launchCount: 1, warmupCount: 1, iterationCount: 3)]
public class SpeechToConversationStreamForwardingBenchmarks
{
    private AudioChunk[] _requestChunks = null!;
    private SpeechConversationEventMessage[] _responseEvents = null!;
    private WebApplication _app = null!;
    private GrpcChannel _channel = null!;
    private SpeechGrpcClient _client = null!;

    [Params(8, 32)]
    public int AudioChunkCount { get; set; }

    [Params(4_096)]
    public int AudioBytesPerChunk { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _requestChunks = CreateAudioChunks(AudioChunkCount, AudioBytesPerChunk);
        _responseEvents = CreateResponseEvents(AudioChunkCount);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddGrpc();
        builder.Services.AddSingleton<SpeechGrpcClient>(_ => new SyntheticLanguageClient(_responseEvents));

        _app = builder.Build();
        _app.UseHostedVoiceGrpc();
        _app.StartAsync().GetAwaiter().GetResult();

        var handler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, _app.GetTestServer().CreateHandler());
        _channel = GrpcChannel.ForAddress(
            "http://localhost",
            new GrpcChannelOptions
            {
                HttpHandler = handler
            });
        _client = new SpeechGrpcClient(_channel);
    }

    [Benchmark]
    public async Task<int> ForwardSpeechToConversationStream()
    {
        using var call = _client.SpeechToConversationStream();

        foreach (var chunk in _requestChunks)
        {
            await call.RequestStream.WriteAsync(chunk).ConfigureAwait(false);
        }

        await call.RequestStream.CompleteAsync().ConfigureAwait(false);

        var responseCount = 0;
        while (await call.ResponseStream.MoveNext(CancellationToken.None).ConfigureAwait(false))
        {
            responseCount++;
        }

        return responseCount;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _channel.Dispose();
        _app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private static AudioChunk[] CreateAudioChunks(int count, int bytesPerChunk)
    {
        var chunks = new AudioChunk[count];
        for (var index = 0; index < chunks.Length; index++)
        {
            chunks[index] = new AudioChunk
            {
                Data = ByteString.CopyFrom(CreatePayload(bytesPerChunk, index)),
                MimeType = AudioMimeType.AudioPcm,
                LanguageCode = "de"
            };
        }

        return chunks;
    }

    private static byte[] CreatePayload(int length, int seed)
    {
        var buffer = new byte[length];
        for (var index = 0; index < buffer.Length; index++)
        {
            buffer[index] = (byte)((index + seed) % 251);
        }

        return buffer;
    }

    private static SpeechConversationEventMessage[] CreateResponseEvents(int count)
    {
        var events = new SpeechConversationEventMessage[count];
        for (var index = 0; index < events.Length; index++)
        {
            events[index] = new SpeechConversationEventMessage
            {
                TranscriptUpdate = new SpeechConversationTranscriptUpdate
                {
                    Text = $"partial-{index}",
                    Confidence = 80,
                    TurnIndex = 0
                }
            };
        }

        return events;
    }
}
