using System.Text.Json;
using System.Text.Json.Serialization;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Mda.Contracts.V1.Variables;

namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Aufgelöstes Formularmodell für die Laufzeit aus FormContractDocument,
/// FieldDefinitions und VariableDefinitions. Sprach- und HTML-Oberfläche nutzen
/// dasselbe normative Verhalten aus Regeln und Validierungsbedingungen.
/// Oberflächenspezifische Angaben bleiben Darstellungshinweise.
/// </summary>
public sealed class FormContract
{
    /// <summary>
    /// Version des JSON-Schemas für das zugrunde liegende Vertragsdokument.
    /// </summary>
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = string.Empty;

    /// <summary>
    /// Inhaltsversion des Vertrags. Semantische Versionierung wird empfohlen.
    /// </summary>
    [JsonPropertyName("contractVersion")]
    public string ContractVersion { get; set; } = string.Empty;

    /// <summary>
    /// Stabile Anwendungskennung.
    /// </summary>
    [JsonPropertyName("applicationId")]
    public string ApplicationId { get; set; } = string.Empty;

    /// <summary>
    /// Stabile Formularkennung innerhalb der Anwendung.
    /// </summary>
    [JsonPropertyName("formId")]
    public FormId FormId { get; set; }

    /// <summary>
    /// Übersetzter Formularname als HTML-Titel und zur gesprochenen Einführung.
    /// </summary>
    [JsonPropertyName("displayName")]
    public LocalizedText? DisplayName { get; set; }

    /// <summary>
    /// Übersetzte Beschreibung, die ohne visuelle Bezüge vorgelesen werden kann.
    /// </summary>
    [JsonPropertyName("description")]
    public LocalizedText? Description { get; set; }

    /// <summary>
    /// Unterstützte Basissprachen als kleingeschriebene ISO-639-1-Kennungen, etwa de und en.
    /// </summary>
    [JsonPropertyName("supportedLanguages")]
    public List<string> SupportedLanguages { get; set; } = [];

    /// <summary>
    /// Kennung der Feldgruppe, mit der der Ablauf beginnt.
    /// </summary>
    [JsonPropertyName("entryFieldGroupId")]
    public FieldGroupId EntryFieldGroupId { get; set; }

    /// <summary>
    /// Bei true gelten fehlende Übersetzungen als Fehler. Es wird nicht still auf eine andere Sprache zurückgegriffen.
    /// </summary>
    [JsonPropertyName("strictLocalization")]
    public bool StrictLocalization { get; set; }

    /// <summary>
    /// Optionale Standardsprache als ISO-639-1-Kennung zur deterministischen Auflösung
    /// von LocalizedText. Wird nur bei ausgeschaltetem StrictLocalization verwendet.
    /// </summary>
    [JsonPropertyName("defaultLanguage")]
    public string? DefaultLanguage { get; set; }

    /// <summary>
    /// Herkunftsverweis auf den eigenständigen Feldkatalog, aus dem <see cref="Fields" />
    /// aufgelöst wurde.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("fieldCatalog")]
    public FieldDefinitionsReference? FieldDefinitions { get; set; }

    /// <summary>
    /// Herkunftsverweis auf den eigenständigen Variablenkatalog, aus dem <see cref="Variables" />
    /// aufgelöst wurde.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("variableCatalog")]
    public VariableDefinitionsReference? VariableDefinitions { get; set; }

    /// <summary>
    /// Variablen halten Werte. Felder erfassen sie, Regeln und Validierungsbedingungen steuern das Verhalten.
    /// </summary>
    [JsonPropertyName("variables")]
    public List<VariableDefinition> Variables { get; set; } = [];

    /// <summary>
    /// Felder referenzieren Variablen und legen Eingabe, Bearbeitbarkeit und Projektionen fest.
    /// </summary>
    [JsonPropertyName("fields")]
    public List<FieldDefinition> Fields { get; set; } = [];

    /// <summary>
    /// Feldgruppen ordnen Felder und untergeordnete Feldgruppen zu einem Ablauf.
    /// </summary>
    [JsonPropertyName("fieldGroups")]
    public List<FieldGroupDefinition> FieldGroups { get; set; } = [];

    /// <summary>
    /// Regeln für bedingtes Verhalten wie Sichtbarkeit, Freigabe und Kopieren von Werten.
    /// </summary>
    [JsonPropertyName("rules")]
    public List<RuleDefinition> Rules { get; set; } = [];

    /// <summary>
    /// Globale Validierungsregeln zusätzlich zu feld- und variablenbezogenen Regeln.
    /// </summary>
    [JsonPropertyName("constraints")]
    public List<ConstraintDefinition> Constraints { get; set; } = [];

    /// <summary>
    /// Optionale Projektionen für Sprache und HTML, die das normative Verhalten nicht ändern dürfen.
    /// </summary>
    [JsonPropertyName("projections")]
    public ContractProjections? Projections { get; set; }

    /// <summary>
    /// Zusätzliche, nicht normative Metadaten.
    /// </summary>
    [JsonPropertyName("extensions")]
    public Dictionary<string, JsonElement>? Extensions { get; set; }
}
