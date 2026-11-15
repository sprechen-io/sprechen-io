namespace Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;

/// <summary>
/// Bildet Sprachcodes auf die Ressourcenkennung ihres Sprachnamens ab.
/// Die typisierte Überladung verwendet <see cref="LanguageCode"/>.
/// Die Stringüberladung normalisiert und prüft den Code selbst.
/// </summary>
public sealed class VoiceLanguageNameResourceIdResolver
{
    public string Resolve(LanguageCode language) =>
        $"voice.languageName.{language.Value}";

    /// <summary>
    /// Bildet den getrimmten und kleingeschriebenen Wert auf eine Ressourcenkennung ab.
    /// Bei null, leerem Wert oder reinem Leerraum wird mit <c>ParamName=isoCode</c> abgebrochen.
    /// </summary>
    public string Resolve(string isoCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        return $"voice.languageName.{isoCode.Trim().ToLowerInvariant()}";
    }
}
