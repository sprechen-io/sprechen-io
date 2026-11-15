using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.Speech.Runtime.Tests.Testing;

internal static class WebSpeechClientServiceCollectionFactory
{
    public static ServiceCollection Create()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<NavigationManager>(new FixedNavigationManager("https://example.test/app/"));
        services.AddSingleton<IJSRuntime>(CreateJsRuntime());
        services.AddSingleton<InMemoryEvaluationTelemetryOutboxStore>();
        services.AddSingleton<IEvaluationTelemetryOutboxStore>(
            static serviceProvider => serviceProvider.GetRequiredService<InMemoryEvaluationTelemetryOutboxStore>());
        services.AddSingleton<RecordingEvaluationTelemetryDeliveryTransport>();
        services.AddSingleton<IEvaluationTelemetryDeliveryTransport>(
            static serviceProvider => serviceProvider.GetRequiredService<RecordingEvaluationTelemetryDeliveryTransport>());
        services.AddSingleton<IEvaluationTelemetryExclusiveLease, ImmediateEvaluationTelemetryExclusiveLease>();
        return services;
    }

    private static FakeJsRuntime CreateJsRuntime()
    {
        var jsRuntime = new FakeJsRuntime();
        jsRuntime.SetResult("userLanguageSettings.get", new UserLanguageSettings(null, null));
        jsRuntime.SetResult("userLanguageSettings.getPreferredLanguageTags", Array.Empty<string>());
        jsRuntime.SetResult("voiceActivationSettings.getWakeWordActivationFormsOverride", Array.Empty<string>());
        jsRuntime.SetResult("pcmPlayer.getVolume", 1f);
        return jsRuntime;
    }
}
