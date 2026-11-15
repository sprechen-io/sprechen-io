namespace Sprechen.Mda.Contracts.V1;

/// <summary>
/// <para>
/// DE: Repräsentiert lokalisierten Text als Zuordnung von Sprachcodes/Varianten
/// (z. B. <c>de</c>, <c>de-CH</c>, <c>de@formal</c>) zum jeweiligen Text.
/// </para>
/// <para>
/// EN: Represents localized text as a map from language codes/variants (e.g. <c>de</c>, <c>de-CH</c>, <c>de@formal</c>) to the corresponding text.
/// </para>
/// </summary>
/// <remarks>
/// <para>
/// DE: Dieser Typ wird in den JSON-Contracts verwendet. Das deterministische Fallback-Verhalten
/// (exakter Treffer, Basissprache, Standardsprache, erste unterstützte Sprache) wird durch
/// <see cref="Localization.LocalizedTextResolver" /> implementiert.
/// </para>
/// <para>
/// EN: This type is used in the JSON contracts. Deterministic fallback behavior (exact match, base language, default language, first supported language) is implemented by
/// <see cref="Localization.LocalizedTextResolver" />.
/// </para>
/// </remarks>
public sealed class LocalizedText : Dictionary<string, string>
{
}
