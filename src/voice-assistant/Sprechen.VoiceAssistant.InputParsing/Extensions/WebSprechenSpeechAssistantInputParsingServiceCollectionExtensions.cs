using Microsoft.Extensions.DependencyInjection;

namespace Sprechen.VoiceAssistant.InputParsing.Extensions;

/// <summary>
/// Registriert Transkriptparser und phonetische Ausgabe mit dem gemeinsamen
/// eingebetteten Lexikon für die Spracheingabe des Assistenten.
/// </summary>
public static class WebSprechenSpeechAssistantInputParsingServiceCollectionExtensions
{
    public static IServiceCollection AddWebSprechenSpeechAssistantInputParsing(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IFormTranscriptLexiconProvider, EmbeddedFormTranscriptLexiconProvider>();
        services.AddSingleton<IFormTranscriptParser, FormTranscriptParser>();
        // Die Datumskomponenten verwenden dieselbe Parserinstanz über einen getrennten Vertrag.
        services.AddSingleton<IFormDateComponentParser>(
            static sp => (FormTranscriptParser)sp.GetRequiredService<IFormTranscriptParser>());
        services.AddSingleton<IPhoneticSpellOutRenderer>(serviceProvider =>
            new PhoneticSpellOutRenderer(serviceProvider.GetRequiredService<IFormTranscriptLexiconProvider>()));
        return services;
    }
}
