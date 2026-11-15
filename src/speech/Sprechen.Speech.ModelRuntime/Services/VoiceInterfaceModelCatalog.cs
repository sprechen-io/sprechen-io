namespace Sprechen.Speech.ModelRuntime.Services;

/// <summary>
/// Erschließt den verzögert geladenen Katalog des Dialogmodells mit
/// Sprachen, Lexikon, Aktivierungswörtern und lokalisierten Textressourcen.
/// </summary>
public static class VoiceInterfaceModelCatalog
{
    private static readonly Lazy<VoiceInterfaceModelCatalogCore> Catalog = new(
        () => new VoiceInterfaceModelCatalogCore(VoiceInterfaceModelLoader.Load()));

    public static string DefaultLanguage => Catalog.Value.DefaultLanguage;

    public static IReadOnlyList<string> SupportedLanguages => Catalog.Value.SupportedLanguages;

    public static bool SessionLanguageOfferEnabled => Catalog.Value.SessionLanguageOfferEnabled;

    public static TimeSpan FirstPcmEnqueueTimeout => Catalog.Value.FirstPcmEnqueueTimeout;

    public static EarconId FirstPcmTimeoutEarconId => Catalog.Value.FirstPcmTimeoutEarconId;

    public static VoiceLexiconModelCatalog Lexicon => Catalog.Value.Lexicon;

    public static VoiceWakeWordModelCatalog WakeWord => Catalog.Value.WakeWord;

    public static VoiceResourceTextModelCatalog Resources => Catalog.Value.Resources;
}
