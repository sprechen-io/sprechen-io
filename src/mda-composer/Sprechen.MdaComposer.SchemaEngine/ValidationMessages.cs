namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Bündelt Meldungsvorlagen für einheitliche Validierungsmeldungen im Composer.
/// </summary>
public static class ValidationMessages
{
    public const string RequiredFieldMissing = "Pflichtfeld fehlt.";
    public const string RequiredPropertyMissing = "Pflichtfeld \"{0}\" fehlt.";
    public const string UnknownProperty = "Unbekannte Eigenschaft \"{0}\".";
    public const string ExpectedObjectGot = "Erwartet: Objekt, erhalten: {0}.";
    public const string ExpectedMapGot = "Erwartet: Objekt (Map), erhalten: {0}.";
    public const string ExpectedArrayGot = "Erwartet: Array, erhalten: {0}.";
    public const string ExpectedStringGot = "Erwartet: String, erhalten: {0}.";
    public const string ExpectedIntegerGot = "Erwartet: Ganzzahl, erhalten: {0}.";
    public const string ExpectedNumberGot = "Erwartet: Zahl, erhalten: {0}.";
    public const string InvalidNumber = "Ungültiger Zahlenwert.";
    public const string ConstExpected = "Konstanter Wert \"{0}\" erwartet, \"{1}\" erhalten.";
    public const string EnumNotAllowed = "Wert \"{0}\" ist nicht in der erlaubten Liste: {1}.";
    public const string MinLength = "Mindestlänge {0}, aktuell {1}.";
    public const string MaxLength = "Maximallänge {0}, aktuell {1}.";
    public const string PatternMismatch = "Wert entspricht nicht dem Muster: {0}";
    public const string PatternTimeout = "Musterpruefung wurde wegen Timeout abgebrochen (moeglicher ReDoS): {0}";
    public const string NotConstraintFailed = "Wert verletzt 'not'-Bedingung.";
    public const string DuplicateArrayItem = "Doppeltes Element im Array (uniqueItems): {0}";
    public const string MinValue = "Minimalwert {0}, aktuell {1}.";
    public const string MaxValue = "Maximalwert {0}, aktuell {1}.";
    public const string MinItems = "Mindestens {0} Elemente erwartet, {1} vorhanden.";
    public const string MaxItems = "Maximal {0} Elemente erlaubt, {1} vorhanden.";
    public const string MinProperties = "Mindestens {0} Eigenschaften erwartet, {1} vorhanden.";
    public const string UnionNoMatch = "Wert passt zu keiner der {0} Varianten ({1}).";
}
