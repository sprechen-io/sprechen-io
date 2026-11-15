using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Sprechen.Speech.App.ClientInfrastructure.Services;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.Web.SpeechClient;
using Sprechen.Voice.App.Tests.TestDoubles;
using Sprechen.Voice.App.Tests.Testing;
using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.Voice.App.Tests.Extensions;

public sealed class WebSprechenClientServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSprechenSpeechWebClient_WhenResolved_ValidatesCompositeRootGraph()
    {
        var services = CreateServices();

        services.AddSprechenSpeechWebClient();

        using var scope = ValidatedServiceProviderScope.Create(services);

        var initializer = scope.GetRequiredService<UserLanguageSettingsInitializer>();
        var languageState = scope.GetRequiredService<IUserLanguageState>();
        var supportedLanguagesClient = scope.GetRequiredService<ISupportedLanguagesClient>();
        var modeAwareSpeechCaptureClient = scope.GetRequiredService<IModeAwareSpeechCaptureClient>();
        var voiceCommandDialog = scope.GetRequiredService<IVoiceCommandDialog>();
        var speechAssistantService = scope.GetRequiredService<ISpeechAssistantService>();
        var sessionController = scope.GetRequiredService<ISpeechAssistantSessionController>();

        Assert.NotNull(initializer);
        Assert.NotNull(languageState);
        Assert.NotNull(supportedLanguagesClient);
        Assert.NotNull(modeAwareSpeechCaptureClient);
        Assert.NotNull(voiceCommandDialog);
        Assert.NotNull(speechAssistantService);
        Assert.NotNull(sessionController);
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<NavigationManager>(new FixedNavigationManager("https://example.test/app/"));

        var jsRuntime = new FakeJsRuntime();
        jsRuntime.SetResult("userLanguageSettings.get", new UserLanguageSettings(null, null));
        jsRuntime.SetResult("userLanguageSettings.getPreferredLanguageTags", Array.Empty<string>());
        jsRuntime.SetResult("voiceActivationSettings.getWakeWordActivationFormsOverride", Array.Empty<string>());
        jsRuntime.SetResult("pcmPlayer.getVolume", 1f);
        services.AddSingleton<IJSRuntime>(jsRuntime);
        services.AddSingleton<IEvaluationTelemetryOutboxStore, InMemoryEvaluationTelemetryOutboxStore>();
        services.AddSingleton<IEvaluationTelemetryDeliveryTransport, AcceptedEvaluationTelemetryDeliveryTransport>();
        services.AddSingleton<IEvaluationTelemetryExclusiveLease, ImmediateEvaluationTelemetryExclusiveLease>();

        return services;
    }
}
