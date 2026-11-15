namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Liefert zwei Sprachnamen mit demselben möglichen Näherungstreffer.
/// Slowanisch unterscheidet sich von Slowakisch und Slowenisch jeweils um ein Zeichen,
/// sodass die Zurückweisung einer mehrdeutigen Sprachwahl geprüft werden kann.
/// </summary>
internal sealed class CollidingLanguageCatalog : ILanguageCatalog
{
    private static readonly LanguageCode Slovak = LanguageCode.ParseIso6391("sk");
    private static readonly LanguageCode Slovenian = LanguageCode.ParseIso6391("sl");

    public LanguageCode DefaultLanguage => Slovak;

    public IReadOnlyList<LanguageCode> SupportedLanguages => [Slovak, Slovenian];

    public bool IsSupported(LanguageCode language)
        => language == Slovak || language == Slovenian;

    public string GetEndonym(LanguageCode language)
        => language == Slovak ? "Slowakisch" : "Slowenisch";

    public string GetDisplayName(LanguageCode language, LanguageCode displayLanguage)
        => GetEndonym(language);
}
