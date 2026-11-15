namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Platzhalter für vorgeschriebene, nicht leere Kennungsfelder der Nutzlast,
/// wenn kein entsprechendes Objekt vorhanden ist.
/// </summary>
public static class TelemetryPayloadPlaceholders
{
    /// <summary>
    /// Platzhalter für ein Feld oder eine Feldgruppe, wenn kein aktuelles Objekt vorliegt,
    /// etwa weil alle Felder durch Regeln verborgen sind.
    /// Er bezeichnet kein Objekt und darf deshalb keinen kanonischen Übergang erzeugen.
    /// Die Projektion behandelt ihn wie eine fehlende Kennung. Andernfalls würden verschiedene
    /// unbekannte Objekte unter demselben Deduplizierungsschlüssel zusammenfallen.
    /// </summary>
    public const string UnidentifiedObject = "n/a";
}
