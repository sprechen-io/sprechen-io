using Microsoft.Extensions.DependencyInjection;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.VoiceRuntime.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.VoiceRuntime.Extensions;

/// <summary>
/// Registriert Aufnahme und Sprachausgabe mit gemeinsam genutztem
/// Versuchsbeobachter und Wiederholungspuffer im jeweiligen DI-Gültigkeitsbereich.
/// </summary>
public static class WebSprechenSpeechAssistantVoiceRuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddWebSprechenSpeechAssistantVoiceRuntime(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<FormVoiceCommandNoInputFeedbackObserver>();
        services.AddScoped<IFormVoiceCommandAttemptObserver>(
            provider => provider.GetRequiredService<FormVoiceCommandNoInputFeedbackObserver>());
        services.AddScoped<FormVoiceCommandSpokenOutputReplayBuffer>();
        services.AddScoped<IFormVoiceCommandSpokenOutputReplayBuffer>(
            provider => provider.GetRequiredService<FormVoiceCommandSpokenOutputReplayBuffer>());
        services.AddScoped<IFormVoiceCommandRecordingService, FormVoiceCommandRecordingService>();
        services.AddScoped<IFormVoiceCommandSpeechService, FormVoiceCommandSpeechService>();

        return services;
    }
}
