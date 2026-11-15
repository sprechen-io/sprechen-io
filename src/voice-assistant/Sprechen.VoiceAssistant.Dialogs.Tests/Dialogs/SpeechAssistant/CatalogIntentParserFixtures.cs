using Sprechen.Speech.LanguageContracts;
using Sprechen.Speech.ModelRuntime.Services;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt den Sprachkatalog der Anwendung und die Auflösung von Sprachwechselzielen für Tests bereit.
/// So verwenden die Tests die unterstützten Sprachen und Namen des Produkts statt eigener Wortlisten.
/// </summary>
internal static class CatalogIntentParserFixtures
{
    internal static ILanguageCatalog LanguageCatalog { get; } = VoiceInterfaceLanguageCatalog.CreateDefault();

    internal static LanguageChangeTargetResolver LanguageTargets { get; } = new(LanguageCatalog);

    internal static IReadOnlyList<string> SupportedIsoCodes { get; } = LanguageCatalog.SupportedLanguages
        .Select(static language => language.Value)
        .ToArray();
}
