using Microsoft.Extensions.DependencyInjection;

namespace Sprechen.VoiceAssistant.VoiceCommands.Extensions;

/// <summary>
/// Registriert die Verarbeitung globaler Formularbefehle und die Weiterleitung
/// an die jeweils zuständige Funktion.
/// </summary>
public static class WebSprechenSpeechAssistantVoiceCommandsServiceCollectionExtensions
{
    public static IServiceCollection AddWebSprechenSpeechAssistantVoiceCommands(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IFormVoiceCommandGlobalCommandRouter, FormVoiceCommandGlobalCommandRouter>();
        services.AddScoped<IFormVoiceCommandGlobalCommandHandler, FormVoiceCommandNavigationHandler>();
        services.AddScoped<IFormVoiceCommandGlobalCommandHandler, FormVoiceCommandRepeatHandler>();
        services.AddScoped<IFormVoiceCommandGlobalCommandHandler, FormVoiceCommandHelpHandler>();
        services.AddScoped<IFormVoiceCommandGlobalCommandHandler, FormVoiceCommandStatusHandler>();
        services.AddScoped<IFormVoiceCommandGlobalCommandHandler, FormVoiceCommandDiscoveryHandler>();
        services.AddScoped<IFormVoiceCommandGlobalCommandHandler, FormVoiceCommandJumpHandler>();
        services.AddScoped<IFormVoiceCommandGlobalCommandHandler, FormVoiceCommandInputModeHandler>();
        services.AddScoped<IFormVoiceCommandGlobalCommandHandler, FormVoiceCommandSubmitHandler>();
        services.AddScoped<IFormVoiceCommandGlobalCommandHandler, FormVoiceCommandFormStatusHandler>();

        return services;
    }
}
