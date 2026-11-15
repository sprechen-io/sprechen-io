namespace Sprechen.Mda.Contracts.V1.Catalog;

/// <summary>
/// Kanonische Regeln für Treffer im Auswahldialog für Schema, Vorprüfung und Laufzeit.
/// </summary>
public static class CatalogVoiceIntentMatchMode
{
    /// <summary>Treffer, sobald die Äußerung einer ganzen normalisierten Phrase gleicht.</summary>
    public const string Phrase = "phrase";

    /// <summary>Treffer, sobald die Äußerung mit einem normalisierten Präfix beginnt.</summary>
    public const string Prefix = "prefix";
}
