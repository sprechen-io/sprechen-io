using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Typisierte Arten von Validierungsregeln gemäß constraint-language.schema.json.
/// Die Serialisierung verwendet Werte aus <see cref="ConstraintKinds"/>.
/// <see cref="Unknown"/> kennzeichnet unbekannte Werte und darf nicht serialisiert werden.
/// </summary>
[JsonConverter(typeof(ConstraintKindJsonConverter))]
public enum ConstraintKind
{
    /// <summary>
    /// Standardwert für fehlende oder unbekannte Regelarten. Die Vorprüfung meldet ihn.
    /// Er darf nicht als Übertragungswert geschrieben werden.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Pflichtwert. Übertragungswert required.
    /// </summary>
    Required,

    /// <summary>
    /// Musterprüfung. Übertragungswert pattern.
    /// </summary>
    Pattern,

    /// <summary>
    /// Mindestlänge. Übertragungswert minLength.
    /// </summary>
    MinLength,

    /// <summary>
    /// Höchstlänge. Übertragungswert maxLength.
    /// </summary>
    MaxLength,

    /// <summary>
    /// Untergrenze. Übertragungswert min.
    /// </summary>
    Min,

    /// <summary>
    /// Obergrenze. Übertragungswert max.
    /// </summary>
    Max,

    /// <summary>
    /// Wertebereich. Übertragungswert range.
    /// </summary>
    Range,

    /// <summary>
    /// Frühestes Datum. Übertragungswert dateMin.
    /// </summary>
    DateMin,

    /// <summary>
    /// Spätestes Datum. Übertragungswert dateMax.
    /// </summary>
    DateMax,

    /// <summary>
    /// Datumsbereich. Übertragungswert dateRange.
    /// </summary>
    DateRange,

    /// <summary>
    /// Gleichheit. Übertragungswert equals.
    /// </summary>
    Equals,

    /// <summary>
    /// Ungleichheit. Übertragungswert notEquals.
    /// </summary>
    NotEquals,

    /// <summary>
    /// Liste zulässiger Werte. Übertragungswert oneOf.
    /// </summary>
    OneOf,

    /// <summary>
    /// Alle Werte oder keiner. Übertragungswert allOrNone.
    /// </summary>
    AllOrNone,

    /// <summary>
    /// Vergleich. Übertragungswert compare.
    /// </summary>
    Compare,

    /// <summary>
    /// Variablenvergleich. Übertragungswert compareVariables.
    /// </summary>
    CompareVariables,

    /// <summary>
    /// Benutzerdefinierte Validierungsregel. Übertragungswert customConstraint.
    /// </summary>
    CustomConstraint
}
