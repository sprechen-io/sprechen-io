using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.CaptureSurface.Extensions;

/// <summary>
/// Registriert Aufnahmeanzeige und Betätigungssteuerung mit gemeinsamen Sitzungsinstanzen,
/// damit ihre unterschiedlichen Zugänge denselben Zustand und Controller verwenden.
/// </summary>
public static class SpeechCaptureSurfaceServiceCollectionExtensions
{
    public static IServiceCollection AddSpeechCaptureSurface(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSpeechCaptureSurfaceState();
        services.AddSpeechCaptureSurfaceControllers();

        return services;
    }

    public static IServiceCollection AddSpeechCaptureSurfaceState(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<SpeechCaptureSurfaceStateStore>();
        services.TryAddScoped<ISpeechCaptureSurfaceState>(
            sp => sp.GetRequiredService<SpeechCaptureSurfaceStateStore>());

        return services;
    }

    public static IServiceCollection AddSpeechCaptureSurfaceControllers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<SpeechCaptureSurfaceController>();
        services.TryAddScoped<ISpeechCaptureSurfaceController>(
            sp => sp.GetRequiredService<SpeechCaptureSurfaceController>());
        services.TryAddScoped<ISpeechCaptureSurfacePressController>(
            sp => sp.GetRequiredService<SpeechCaptureSurfaceController>());

        return services;
    }
}
