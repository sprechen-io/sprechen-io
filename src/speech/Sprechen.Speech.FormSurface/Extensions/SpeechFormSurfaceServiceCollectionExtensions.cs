using Microsoft.Extensions.DependencyInjection;

namespace Sprechen.Speech.FormSurface.Extensions;

/// <summary>
/// Registriert Schreib- und Beobachterzugang auf demselben Formularoberflächenspeicher,
/// damit veröffentlichte Zustände unmittelbar über den gemeinsamen Dienst verfügbar sind.
/// </summary>
public static class SpeechFormSurfaceServiceCollectionExtensions
{
    public static IServiceCollection AddSpeechFormSurface(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<SpeechAssistantFormSurfaceStore>();
        services.AddScoped<ISpeechAssistantFormSurfaceState>(
            sp => sp.GetRequiredService<SpeechAssistantFormSurfaceStore>());
        services.AddScoped<ISpeechAssistantFormSurfaceController>(
            sp => sp.GetRequiredService<SpeechAssistantFormSurfaceStore>());
        services.AddScoped<FormSurfaceSnapshotAssembler>();
        services.AddScoped<SpeechAssistantFormSurfacePublication>();

        return services;
    }
}
