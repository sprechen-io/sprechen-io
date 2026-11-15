using Sprechen.Speech.LanguageContracts;
using Sprechen.Speech.ModelRuntime.Services;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt den produktiven Sprachkatalog und die Sprachzielauflösung für Parserprüfungen bereit.
/// So verwenden Tests für Sprachwechsel dieselben modellierten Bezeichnungen wie die Anwendung.
/// </summary>
internal static class CatalogIntentParserFixtures
{
    internal static ILanguageCatalog LanguageCatalog { get; } = VoiceInterfaceLanguageCatalog.CreateDefault();

    internal static LanguageChangeTargetResolver LanguageTargets { get; } = new(LanguageCatalog);

    internal static IReadOnlyList<string> SupportedIsoCodes { get; } = LanguageCatalog.SupportedLanguages
        .Select(static language => language.Value)
        .ToArray();
}
