using Microsoft.Extensions.DependencyInjection;
using Sprechen.Contracts.FormSurfaces;
using Sprechen.Speech.App.ClientInfrastructure.Services;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.CaptureSurface.Extensions;
using Sprechen.Speech.FormSurface.Extensions;
using Sprechen.Web.FormSurfaces;

namespace Sprechen.Web.SpeechClient;

/// <summary>
/// Registriert Formular- und Aufnahmeoberflächen sowie ihre Sitzungssteuerung im DI-Container.
/// </summary>
public static class WebSpeechClientSurfaceServiceCollectionExtensions
{
    public static IServiceCollection AddWebSprechenSpeechAssistantSurfaces(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IFormSurfaceDefinitionBuilder, FormSurfaceDefinitionBuilder>();
        services.AddSingleton<IFormSurfacePresenter, FormSurfacePresenter>();
        services.AddSpeechCaptureSurfaceState();
        services.AddSpeechFormSurface();

        return services;
    }

    public static IServiceCollection AddWebSprechenSpeechAssistantSurfaceControllers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSpeechCaptureSurfaceControllers();
        services.AddScoped<SpeechAssistantSessionController>();
        services.AddScoped<ISpeechAssistantSessionController>(
            sp => sp.GetRequiredService<SpeechAssistantSessionController>());

        return services;
    }
}
