using Microsoft.Extensions.DependencyInjection;

namespace Sprechen.Speech.App.MicrophoneRecording.Extensions;

/// <summary>
/// Registriert den Browseradapter der Mikrofonsitzung, damit Aufnahme, Spurverarbeitung
/// und Mikrofonsperre dieselbe Instanz verwenden.
/// </summary>
public static class MicrophoneRecordingServiceCollectionExtensions
{
    public static IServiceCollection AddMicrophoneRecording(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<MicrophoneRecorder>();
        services.AddScoped<IMicrophoneRecorder>(serviceProvider => serviceProvider.GetRequiredService<MicrophoneRecorder>());
        services.AddScoped<ICaptureTrackProcessingReader>(serviceProvider => serviceProvider.GetRequiredService<MicrophoneRecorder>());
        services.AddScoped<IMicGateBrowserBridge>(serviceProvider => serviceProvider.GetRequiredService<MicrophoneRecorder>());
        return services;
    }
}
