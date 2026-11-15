using Microsoft.Extensions.DependencyInjection;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.Speech.ModelRuntime.Services;

namespace Sprechen.VoiceAssistant.FormEngine.Extensions;

/// <summary>
/// Registriert Parser, Reparatur- und Laufzeitdienste der Formularverarbeitung
/// für die Zusammensetzung des Sprachassistenten.
/// </summary>
public static class WebSprechenSpeechAssistantFormEngineServiceCollectionExtensions
{
    public static IServiceCollection AddWebSprechenSpeechAssistantFormEngine(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IYesNoTranscriptClassifier, YesNoTranscriptClassifier>();
        services.AddSingleton<FormRepairReasonResolver>();
        services.AddSingleton<INegationParticleResolver, ModeledNegationParticleResolver>();
        services.AddScoped<IFormDialogRuntimeFactory, FormDialogRuntimeFactory>();
        return services;
    }
}
