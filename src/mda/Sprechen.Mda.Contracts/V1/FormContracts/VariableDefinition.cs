using System.Text.Json;
using System.Text.Json.Serialization;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Aufgelöste Variablendefinition für die Werte eines Formulars. Ein oder mehrere
/// Felder referenzieren sie über variableId. Validierungsregeln prüfen ihre Werte.
/// </summary>
public sealed class VariableDefinition
{
    /// <summary>
    /// Stabile Variablenkennung innerhalb des Vertrags.
    /// </summary>
    [JsonPropertyName("variableId")]
    public VariableId VariableId { get; set; }

    /// <summary>
    /// Stabiler technischer Name der Variablen.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optionaler übersetzter Beschreibungs- oder Hilfetext für die Sprachausgabe.
    /// </summary>
    [JsonPropertyName("description")]
    public LocalizedText? Description { get; set; }

    /// <summary>
    /// Optionale Einheit, beispielsweise persons, minutes oder EUR.
    /// </summary>
    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    /// <summary>
    /// Optionales semantisches Merkmal zur fachlichen Einordnung, etwa personCount oder iban.
    /// Ein Namensraum wie address.postalCode wird empfohlen.
    /// </summary>
    [JsonPropertyName("semanticTag")]
    public string? SemanticTag { get; set; }

    /// <summary>
    /// Optionale Datenschutzklasse: public, personal, sensitive oder secret.
    /// </summary>
    [JsonPropertyName("privacyClass")]
    public PrivacyClass? PrivacyClass { get; set; }

    /// <summary>
    /// Optionale Regeln für Spracheingabe, Rücklesen und stille Eingabe. Ohne Angabe
    /// werden Standardwerte anhand von privacyClass verwendet.
    /// </summary>
    [JsonPropertyName("sensitiveDataPolicy")]
    public SensitiveDataPolicy? SensitiveDataPolicy { get; set; }

    /// <summary>
    /// Werttypdefinition als JSON entsprechend den Schemavarianten, etwa string, integer, boolean, enum oder set.
    /// </summary>
    [JsonPropertyName("valueType")]
    public JsonElement ValueType { get; set; }

    /// <summary>
    /// Optionaler Anfangswert gemäß den kanonischen Kodierungsregeln. Null ist unzulässig.
    /// </summary>
    [JsonPropertyName("initialValue")]
    public JsonElement? InitialValue { get; set; }

    /// <summary>
    /// Immer geltende variablenbezogene Validierungsregeln zusätzlich zu feldbezogenen und globalen Regeln.
    /// </summary>
    [JsonPropertyName("invariantConstraints")]
    public List<JsonElement>? InvariantConstraints { get; set; }

    /// <summary>
    /// Optionale Metadaten mit Anforderungskennungen, Quellen und Begründungen.
    /// </summary>
    [JsonPropertyName("trace")]
    public TraceInfo? Trace { get; set; }

    /// <summary>
    /// Zusätzliche, nicht normative Metadaten.
    /// </summary>
    [JsonPropertyName("extensions")]
    public Dictionary<string, JsonElement>? Extensions { get; set; }
}
