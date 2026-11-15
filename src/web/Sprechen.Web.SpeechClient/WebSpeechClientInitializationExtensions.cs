using Microsoft.Extensions.DependencyInjection;
using Sprechen.Speech.App.ClientInfrastructure.Services;

namespace Sprechen.Web.SpeechClient;

/// <summary>
/// Initialisiert die gespeicherten Spracheinstellungen über den registrierten Dienst.
/// </summary>
public static class WebSpeechClientInitializationExtensions
{
    public static ValueTask InitializeSprechenSpeechWebClientAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return serviceProvider.GetRequiredService<UserLanguageSettingsInitializer>().InitializeAsync(cancellationToken);
    }
}
