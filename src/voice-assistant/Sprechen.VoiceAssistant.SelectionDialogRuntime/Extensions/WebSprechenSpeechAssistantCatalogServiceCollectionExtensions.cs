using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Extensions;

/// <summary>
/// Registriert Katalog- und Formularauswahl mit ihren Parsern, Vergleichern und dem gemeinsamen
/// Sprachschritt. Die Dialogdienste können dadurch über denselben Abhängigkeitscontainer
/// zusammengesetzt werden.
/// </summary>
public static class WebSprechenSpeechAssistantCatalogServiceCollectionExtensions
{
    public static IServiceCollection AddWebSprechenSpeechAssistantCatalog(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IFormsCatalogIntentMatcher, FormsCatalogIntentMatcher>();
        services.AddScoped(sp => new SelectionDialogSpeechTurnRunner(
            sp.GetRequiredService<ISpeechCaptureRecordingIndicator>(),
            sp.GetRequiredService<ISpeechCaptureClient>(),
            sp.GetRequiredService<IModeAwareSpeechCaptureClient>(),
            sp.GetRequiredService<SpeechRecognitionWorker>(),
            sp.GetRequiredService<ITextToSpeechPlayer>(),
            sp.GetRequiredService<ILogger<SelectionDialogSpeechTurnRunner>>()));
        services.TryAddScoped<LanguageChangeTargetResolver>();
        services.AddScoped<FormsCatalogLanguageChangeWorkflow>();
        services.AddScoped<IFormsCatalogIntentParser, FormsCatalogIntentParser>();
        services.AddSingleton<IFormSelectionMatcher, FormSelectionMatcher>();
        services.AddScoped<IFormsCatalogDialog, FormsCatalogDialog>();
        services.AddScoped<IFormSelectionDialog, FormSelectionDialog>();

        return services;
    }
}
