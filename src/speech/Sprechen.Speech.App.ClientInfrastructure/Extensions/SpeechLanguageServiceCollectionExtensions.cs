using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Sprechen.Speech.App.ClientInfrastructure.Extensions;

/// <summary>
/// Registriert Sprachzustand und Sprachkatalog samt Einstellungsablauf
/// über getrennte DI-Einstiegspunkte. Der Clientkern erhält den Sitzungszustand,
/// während der Sprachclient Katalog, Sprachauswahl und Einstellungszugänge zusammensetzt.
/// Die Trennung hält den Clientkern eigenständig nutzbar, ohne ihm die Katalog- und Ablaufdienste aufzuzwingen.
/// </summary>
public static class SpeechLanguageServiceCollectionExtensions
{
    public static IServiceCollection AddSpeechLanguageState(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<UserLanguageState>();
        services.TryAddScoped<IUserLanguageState>(
            serviceProvider => serviceProvider.GetRequiredService<UserLanguageState>());
        services.TryAddScoped<IUserLanguagePreferences>(
            serviceProvider => serviceProvider.GetRequiredService<IUserLanguageState>());
        services.TryAddScoped<UserLanguageSettingsInitializer>();
        return services;
    }

    public static IServiceCollection AddSpeechLanguageCatalogAndSettings(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ILanguageCatalog>(_ => CreateDefaultLanguageCatalog());
        services.AddSingleton<SettingsVoiceLanguageResolver>();
        services.AddScoped<UserLanguageSettingsWorkflow>();
        services.AddScoped<IUserLanguageSelectionWriter>(sp => sp.GetRequiredService<UserLanguageSettingsWorkflow>());
        services.AddScoped<IUserLanguageSettingsWorkflow>(sp => sp.GetRequiredService<UserLanguageSettingsWorkflow>());
        services.AddScoped<ISessionSetupSettingsWorkflow>(sp => sp.GetRequiredService<UserLanguageSettingsWorkflow>());
        return services;
    }

    private static ILanguageCatalog CreateDefaultLanguageCatalog() => VoiceInterfaceLanguageCatalog.CreateDefault();
}
