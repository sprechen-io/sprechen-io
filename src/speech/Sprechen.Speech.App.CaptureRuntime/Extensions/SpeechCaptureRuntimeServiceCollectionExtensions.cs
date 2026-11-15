using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Sprechen.Speech.App.CaptureRuntime.Extensions;

/// <summary>
/// Registriert Aufnahmelaufzeit und gemeinsam genutzte Puffer- und Wiedergabezustände,
/// damit aktive und kontinuierliche Aufnahme dieselben Sitzungsdienste verwenden.
/// </summary>
public static class SpeechCaptureRuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddSpeechCaptureRuntime(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<PcmSpeechCaptureSegmenterOptions>();
        services.AddOptions<SpeechCaptureClientOptions>();

        services.TryAddScoped<IEarconPlaybackState, EarconPlaybackState>();
        services.TryAddScoped<ITextToSpeechPlaybackState, TextToSpeechPlaybackState>();
        services.TryAddScoped<IAssistantPlaybackOutputLatencyState, AssistantPlaybackOutputLatencyState>();
        services.TryAddScoped<AssistantPlaybackCoordinator>();
        services.TryAddScoped<SpeechCaptureSilenceDebounceResolver>();
        services.TryAddScoped<SpeechCaptureSpeechAmplitudeThresholdResolver>();
        services.TryAddScoped<SpeechCaptureSegmenterOptionsFactory>();
        services.TryAddScoped<SpeechCaptureSegmenterFactory>();
        services.TryAddScoped<SpeechAudioMimeTypeResolver>();
        services.TryAddScoped<InMemorySpeechSampleRing>();
        services.TryAddScoped<SpeechSampleRing>(sp => sp.GetRequiredService<InMemorySpeechSampleRing>());
        services.TryAddScoped<SpeechCaptureSessionCoordinator>();
        services.TryAddScoped<SpeechCaptureActiveCaptureCoordinator>();
        services.TryAddScoped<SpeechCaptureChunkDispatcher>();
        services.TryAddScoped<SpeechCaptureLocalSessionLifecycle>();
        services.TryAddScoped<ISpeechCaptureLocalSessionChunkSourceFactory, SpeechCaptureLocalSessionChunkSourceFactory>();
        services.TryAddScoped<ActiveSegmentCoordinator>();
        services.TryAddScoped<ContinuousSegmentStreamSource>();
        services.TryAddScoped<IContinuousSegmentStreamSource>(sp => sp.GetRequiredService<ContinuousSegmentStreamSource>());
        services.TryAddScoped<SpeechCaptureClient>();
        services.TryAddScoped<ISpeechCaptureClient>(sp => sp.GetRequiredService<SpeechCaptureClient>());
        services.TryAddScoped<IModeAwareSpeechCaptureClient>(sp => sp.GetRequiredService<SpeechCaptureClient>());

        return services;
    }
}
