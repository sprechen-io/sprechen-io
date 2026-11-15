using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.Services;

namespace Sprechen.VoiceAssistant.Extensions;

/// <summary>
/// Führt die Dienstregistrierungen des Sprachassistenten zusammen,
/// einschließlich Uhr, Einstiegsvokabular und Telemetriekontext.
/// </summary>
public static class WebSprechenDialogServiceCollectionExtensions
{
    public static IServiceCollection AddWebSprechenDialogs(this IServiceCollection services)
        => AddWebSprechenDialogs(services, configuration: null);

    public static IServiceCollection AddWebSprechenDialogs(
        this IServiceCollection services,
        IConfiguration? configuration)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton<ISessionEntryCancelVocabulary, VoiceCommandCatalogCancelVocabulary>();
        services.AddScoped<IStudyTelemetryContext, StudyTelemetryContext>();
        services.AddScoped<IOperationalProbeSession, Sprechen.VoiceAssistant.EvaluationTelemetry.OperationalProbeSession>();
        services.AddWebSprechenSpeechAssistantTelemetry(configuration);
        services.AddWebSprechenSpeechAssistantCatalog();
        services.AddWebSprechenSpeechAssistantInputParsing();
        services.AddWebSprechenSpeechAssistantFormEngine();
        services.AddWebSprechenSpeechAssistantVoiceRuntime();
        services.AddWebSprechenSpeechAssistantVoiceCommands();
        services.AddWebSprechenSpeechAssistantVoiceInteraction();
        services.AddWebSprechenSpeechAssistantOrchestration();

        return services;
    }
}
