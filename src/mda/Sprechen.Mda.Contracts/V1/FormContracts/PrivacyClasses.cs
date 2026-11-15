namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Zulässige Datenschutzklassen gemäß privacy-policy.schema.json.
/// </summary>
public static class PrivacyClasses
{
    /// <summary>Nicht sensible Daten. Diese Schutzklasse erlaubt Sprechen und Protokollieren.</summary>
    public const string Public = "public";

    /// <summary>Personenbezogene Daten dürfen gesprochen, aber nicht unmaskiert protokolliert werden.</summary>
    public const string Personal = "personal";

    /// <summary>Sensible Daten dürfen nur nach ausdrücklicher Zustimmung zurückgelesen werden.</summary>
    public const string Sensitive = "sensitive";

    /// <summary>Geheime Daten dürfen weder zurückgesprochen noch protokolliert werden.</summary>
    public const string Secret = "secret";
}
