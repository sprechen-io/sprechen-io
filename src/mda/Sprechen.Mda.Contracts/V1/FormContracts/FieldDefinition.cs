using System.Text.Json;
using System.Text.Json.Serialization;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Aufgelöste Felddefinition für Erfassung, Bearbeitung und Darstellung einer Variablen.
/// </summary>
public sealed class FieldDefinition
{
    /// <summary>
    /// Optionale Referenz auf die wiederverwendbare Felddefinition, aus der dieses Feld stammt.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("fieldDefinitionId")]
    public string? FieldDefinitionId { get; set; }

    /// <summary>
    /// Stabile Feldkennung innerhalb des Vertrags.
    /// </summary>
    [JsonPropertyName("fieldId")]
    public FieldId FieldId { get; set; }

    /// <summary>
    /// Kennung der Variablen, die dieses Feld liest und schreibt.
    /// </summary>
    [JsonPropertyName("variableId")]
    public VariableId VariableId { get; set; }

    /// <summary>
    /// Übersetzte Feldbeschriftung als primärer zugänglicher Name in HTML.
    /// Dafür ist ein label-Element einem aria-label vorzuziehen.
    /// </summary>
    [JsonPropertyName("label")]
    public LocalizedText Label { get; set; } = new();

    /// <summary>
    /// Optionaler übersetzter Hilfetext, in HTML üblicherweise über aria-describedby zugeordnet.
    /// </summary>
    [JsonPropertyName("helpText")]
    public LocalizedText? HelpText { get; set; }

    /// <summary>
    /// Optionaler übersetzter Platzhalter für ein HTML-Eingabefeld.
    /// </summary>
    [JsonPropertyName("placeholder")]
    public LocalizedText? Placeholder { get; set; }

    /// <summary>
    /// Übersetzte Beispiele zum Vorlesen oder Anzeigen als Hilfe.
    /// In HTML üblicherweise über aria-describedby zugeordnet.
    /// </summary>
    [JsonPropertyName("examples")]
    public List<LocalizedText>? Examples { get; set; }

    /// <summary>
    /// Bearbeitbarkeit aus Sicht der Nutzerinnen und Nutzer.
    /// </summary>
    [JsonPropertyName("editability")]
    public FieldEditability Editability { get; set; }

    /// <summary>
    /// Bei true wird eine gebundene Zeichenkette als Code statt als Freitext behandelt.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("codeStyle")]
    public bool CodeStyle { get; set; }

    /// <summary>
    /// Bevorzugte Eingabemodi als Hinweise für Oberflächen, beispielsweise HTML-inputmode.
    /// </summary>
    [JsonPropertyName("preferredInputModes")]
    public List<string>? PreferredInputModes { get; set; }

    /// <summary>
    /// Feldbezogene Validierungsregeln zusätzlich zu variablenbezogenen und globalen Regeln.
    /// </summary>
    [JsonPropertyName("fieldConstraints")]
    public List<ConstraintDefinition>? FieldConstraints { get; set; }

    /// <summary>
    /// Optionale, nicht normative Hinweise zur Anordnung der Oberfläche.
    /// </summary>
    [JsonPropertyName("presentationHints")]
    public FieldPresentationHints? PresentationHints { get; set; }

    /// <summary>
    /// Optionale feldbezogene Projektionen für HTML und Sprache.
    /// </summary>
    [JsonPropertyName("projections")]
    public FieldProjections? Projections { get; set; }

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
