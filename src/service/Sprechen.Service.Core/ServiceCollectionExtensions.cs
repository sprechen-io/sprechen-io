using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sprechen.Service.Core.Services;

namespace Sprechen.Service.Core;

/// <summary>
/// Registriert Sprachkatalog, Orchestrierung, Konfigurationsprüfung und Sprachdienstschnittstellen.
/// </summary>
public static class SpeechCoreServiceCollectionExtensions
{
    public static IServiceCollection AddLanguageCore(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IValidateOptions<SpeechProviderOptions>, SpeechProviderOptionsSelectionValidator>();
        services.AddOptions<SpeechProviderOptions>()
            .Bind(configuration.GetSection(SpeechProviderOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<SupportedLanguagesOptions>, SupportedLanguagesOptionsSelectionValidator>();
        services.AddOptions<SupportedLanguagesOptions>()
            .Bind(configuration.GetSection(SupportedLanguagesOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<ILanguageCatalog, LanguageCatalog>();
        services.AddSingleton<ISupportedLanguagesService, SupportedLanguagesService>();

        services.AddSingleton<SpeechTranscriptionOrchestrator>();
        services.AddSingleton<SpeechSynthesisOrchestrator>();
        services.AddSingleton<SpeechConversationStreamOrchestrator>();

        // Alle drei spezialisierten Schnittstellen verwenden dieselbe SpeechService-Instanz.
        services.AddSingleton<ISpeechService, SpeechService>();
        services.AddSingleton<ISpeechToTextService>(sp => sp.GetRequiredService<ISpeechService>());
        services.AddSingleton<IConversationalSpeechToTextService>(sp => sp.GetRequiredService<ISpeechService>());
        services.AddSingleton<ITextToSpeechService>(sp => sp.GetRequiredService<ISpeechService>());

        services.AddHealthChecks()
            .AddCheck<SpeechProviderHealthCheck>("speech-providers", tags: new[] { "ready" });

        return services;
    }
}
