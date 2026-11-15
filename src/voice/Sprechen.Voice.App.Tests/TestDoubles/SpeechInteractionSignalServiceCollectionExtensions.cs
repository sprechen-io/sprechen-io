using Microsoft.Extensions.DependencyInjection;

namespace Sprechen.Voice.App.Tests.TestDoubles;

internal static class SpeechInteractionSignalServiceCollectionExtensions
{
    public static ClientMessageBusSpeechInteractionSignals AddSpeechInteractionSignals(
        this IServiceCollection services,
        ClientMessageBus messageBus)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(messageBus);

        var signals = new ClientMessageBusSpeechInteractionSignals(messageBus);

        services.AddSingleton<IClientMessageBus>(messageBus);
        services.AddSingleton(signals);
        services.AddSingleton<ISpeechInteractionSignals>(signals);
        services.AddSingleton<ISpeechCaptureFeedback>(signals);
        services.AddSingleton<ISpeechCaptureRecordingIndicator>(signals);
        services.AddSingleton<ISpeechBargeInSignals>(signals);
        services.AddSingleton<IEarconFamilySignals>(signals);
        services.AddSingleton<ISpeechCaptureStoppedSignals>(signals);
        services.AddSingleton<ISpeechWakeListeningFallbackSignals>(signals);
        services.AddSingleton<SpeechCaptureSurfaceStateStore>();
        services.AddSingleton<ISpeechCaptureSurfaceState>(sp => sp.GetRequiredService<SpeechCaptureSurfaceStateStore>());

        return signals;
    }
}
