using System.Globalization;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Delegiert die Kulturauswahl der Eingabeparser an die Auflösung gesprochener
/// Zeitwerte, damit Einlesen und Vorlesen dieselbe Sprachzuordnung verwenden.
/// </summary>
internal sealed class FormTemporalCultureResolver
{
    public CultureInfo Resolve(string isoCode)
        => FormTemporalSpokenValueResolver.ResolveCulture(isoCode);
}
