using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ermittelt die Auswahlabsicht eines Transkripts anhand von Katalog, Sprache und Höflichkeitsstufe.
/// </summary>
public interface IFormsCatalogIntentParser
{
    FormsCatalogIntentResult Parse(
        FormsCatalog catalog,
        string transcript,
        string isoCode,
        PolitenessLevel politenessLevel);
}

