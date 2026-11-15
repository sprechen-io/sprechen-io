using Sprechen.Speech.LanguageContracts;
using Sprechen.Speech.ModelRuntime.Services;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Gemeinsamer Sprachkatalog und Zielresolver für Tests des Katalogparsers.
/// Die echte Produktionswortwahl verhindert, dass Sprachwechselkommandos und
/// alleinstehende Sprachnamen gegen erfundenes Testvokabular geprüft werden.
/// </summary>
internal static class CatalogIntentParserFixtures
{
    internal static ILanguageCatalog LanguageCatalog { get; } = VoiceInterfaceLanguageCatalog.CreateDefault();

    internal static LanguageChangeTargetResolver LanguageTargets { get; } = new(LanguageCatalog);

    internal static IReadOnlyList<string> SupportedIsoCodes { get; } = LanguageCatalog.SupportedLanguages
        .Select(static language => language.Value)
        .ToArray();
}
