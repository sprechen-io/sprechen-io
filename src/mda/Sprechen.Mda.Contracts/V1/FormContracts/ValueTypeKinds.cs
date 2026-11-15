namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Zulässige Werttypen von Variablen gemäß variable-definitions.schema.json.
/// </summary>
public static class ValueTypeKinds
{
    public const string String = "string";

    public const string Boolean = "boolean";

    public const string Integer = "integer";

    public const string Date = "date";

    /// <summary>
    /// time bezeichnet eine Uhrzeit ohne Datum.
    /// </summary>
    public const string Time = "time";

    /// <summary>
    /// enum: Ein Wert aus einer festgelegten Menge.
    /// </summary>
    public const string Enum = "enum";

    /// <summary>
    /// set: Mehrere Werte aus einer festgelegten Menge.
    /// </summary>
    public const string Set = "set";

    /// <summary>
    /// decimal: Dezimalzahl als Erweiterungspunkt. Von der Referenzimplementierung nicht unterstützt.
    /// </summary>
    public const string Decimal = "decimal";

    /// <summary>
    /// dateTime: Datum mit Uhrzeit als Erweiterungspunkt. Von der Referenzimplementierung nicht unterstützt.
    /// </summary>
    public const string DateTime = "dateTime";

    /// <summary>
    /// object: Strukturiertes Objekt als Erweiterungspunkt. Von der Referenzimplementierung nicht unterstützt.
    /// </summary>
    public const string Object = "object";

    /// <summary>
    /// list: Werteliste als Erweiterungspunkt. Von der Referenzimplementierung nicht unterstützt.
    /// </summary>
    public const string List = "list";

    /// <summary>
    /// custom: Benutzerdefinierter Typ als Erweiterungspunkt. Von der Referenzimplementierung nicht unterstützt.
    /// </summary>
    public const string Custom = "custom";
}
