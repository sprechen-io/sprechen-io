using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Typisierte Datenschutzklassen gemäß privacy-policy.schema.json. Die Serialisierung
/// verwendet <see cref="PrivacyClasses"/>. Unknown kennzeichnet angegebene, aber
/// nicht erkannte Werte. Eine fehlende Klasse wird als null dargestellt.
/// </summary>
[JsonConverter(typeof(PrivacyClassJsonConverter))]
public enum PrivacyClass
{
    /// <summary>
    /// Angegebene, aber nicht erkannte Datenschutzklasse. Vorprüfung und Laufzeit
    /// lehnen sie ab. Sie darf nicht serialisiert werden.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Nicht sensible Daten. Übertragungswert public.
    /// </summary>
    Public,

    /// <summary>
    /// Personenbezogene Daten. Übertragungswert personal.
    /// </summary>
    Personal,

    /// <summary>
    /// Sensible Daten. Übertragungswert sensitive.
    /// </summary>
    Sensitive,

    /// <summary>
    /// Geheime Daten. Übertragungswert secret.
    /// </summary>
    Secret
}
