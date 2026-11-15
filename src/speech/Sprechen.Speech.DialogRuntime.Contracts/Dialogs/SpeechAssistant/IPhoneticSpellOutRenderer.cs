namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Gibt einen Wert mit den Wörtern des Buchstabieralphabets wieder, etwa
/// <c>K</c> als <c>Kaufmann</c>. Der Vertrag liegt in der Dialoglaufzeit, damit die
/// Bestätigungsansage die Ausgabe nutzen kann, ohne vom Eingabeparser abzuhängen.
/// </summary>
public interface IPhoneticSpellOutRenderer
{
    /// <summary>
    /// Spricht jedes Zeichen mit dem zugehörigen Wort des Buchstabieralphabets, etwa
    /// B wie Berta auf Deutsch oder B for Bravo auf Englisch. Deutsch verwendet das nationale
    /// Alphabet, die übrigen unterstützten Sprachen das ICAO/NATO-Alphabet. Regionstags werden
    /// auf den Basiscode normalisiert. Unbekannte Sprachen verwenden Englisch. Punkte zwischen
    /// den Elementen sollen hörbare Pausen erzeugen. Modellierte technische Zeichen werden
    /// mit ihrem Namen ausgegeben, andere Zeichen bleiben unverändert.
    /// </summary>
    string Render(string value, string isoCode);
}
