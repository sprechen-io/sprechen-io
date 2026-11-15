namespace Sprechen.Mda.Contracts.V1.FormSurfaces;

/// <summary>
/// Unterscheidet sichtbare Feldwerte und Hinweise auf gespeicherte Einträge.
/// Unspecified kennzeichnet einen noch nicht gesetzten Wert. Der JSON-Konverter lehnt ihn ab.
/// </summary>
public enum FormSurfaceFieldValueKind
{
    Unspecified = 0,
    PublicValue,
    StoredEntryPresence
}
