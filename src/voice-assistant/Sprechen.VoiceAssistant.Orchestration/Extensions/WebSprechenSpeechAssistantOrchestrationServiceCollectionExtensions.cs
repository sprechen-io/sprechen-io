using Microsoft.Extensions.DependencyInjection;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.WakeRuntime.Extensions;

namespace Sprechen.VoiceAssistant.Orchestration.Extensions;

/// <summary>
/// Registriert die Orchestrierung und ihren Adapter für die Aktivierungsworterkennung.
/// Assistentendienst und stille Eingabesteuerung teilen innerhalb einer DI-Scope dieselbe Instanz.
/// </summary>
public static class WebSprechenSpeechAssistantOrchestrationServiceCollectionExtensions
{
    public static IServiceCollection AddWebSprechenSpeechAssistantOrchestration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<SpeechAssistantWakeWordInteractionDependencies>();
        services.AddScoped<SpeechAssistantWakeWordDependencies>();
        services.AddScoped<SpeechAssistantCaptureDependencies>();
        services.AddScoped<SpeechAssistantInteractionDependencies>();
        services.AddScoped<SpeechAssistantSetupDependencies>();
        services.AddScoped<SpeechAssistantDialogDependencies>();
        services.AddScoped<SpeechAssistantContext>();
        services.AddScoped<SpeechAssistantService>();
        services.AddScoped<ISpeechAssistantService>(provider => provider.GetRequiredService<SpeechAssistantService>());
        services.AddScoped<ISpeechAssistantSilentInputController>(provider => provider.GetRequiredService<SpeechAssistantService>());
        services.AddSprechenSpeechAssistantWakeRuntime();
        services.AddScoped<SpeechAssistantWakeWordRuntime>();
        return services;
    }
}
