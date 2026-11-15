namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Interaktionskategorien nach Zusammenführung des Variablentyps mit feldbezogenen Verfeinerungen.
/// </summary>
public static class EffectiveFieldInteractionTypes
{
    /// <summary>Abgeleitete Laufzeitinteraktion für freie Texteingaben.</summary>
    public const string Text = "text";
    /// <summary>Abgeleitete Laufzeitinteraktion für boolesche Werte.</summary>
    public const string Boolean = "boolean";
    /// <summary>Abgeleitete Laufzeitinteraktion für ganzzahlige Werte.</summary>
    public const string Number = "number";
    /// <summary>Abgeleitete Laufzeitinteraktion für codeartige Zeichenketten.</summary>
    public const string Code = "code";
    /// <summary>Abgeleitete Laufzeitinteraktion für eine Einfachauswahl aus vorgegebenen Werten.</summary>
    public const string SingleSelect = "singleSelect";
    /// <summary>Abgeleitete Laufzeitinteraktion für Mengenwerte mit Mehrfachauswahl.</summary>
    public const string MultiSelect = "multiSelect";
    /// <summary>Abgeleitete Laufzeitinteraktion für Datumswerte.</summary>
    public const string Date = "date";
    /// <summary>Abgeleitete Laufzeitinteraktion für Uhrzeitwerte.</summary>
    public const string Time = "time";
}
