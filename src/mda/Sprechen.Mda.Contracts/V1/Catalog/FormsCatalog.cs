using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.Catalog;

/// <summary>
/// Lokal aufgelöster Anwendungskatalog für Interpreter und Renderer. Verbindet
/// FormsCatalogDocument, ApplicationSelectionDialog und die zugehörigen Profile.
/// Das übertragene JSON-Dokument wird durch FormsCatalogDocument beschrieben.
/// </summary>
public sealed class FormsCatalog
{
    /// <summary>
    /// Version des JSON-Schemas zur Validierung des zugrunde liegenden Katalogdokuments.
    /// </summary>
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = string.Empty;

    /// <summary>
    /// Version des Kataloginhalts. Semantische Versionierung wird empfohlen.
    /// </summary>
    [JsonPropertyName("catalogVersion")]
    public string CatalogVersion { get; set; } = string.Empty;

    /// <summary>
    /// Optionaler Generierungszeitpunkt. ISO-8601 wird empfohlen.
    /// </summary>
    [JsonPropertyName("generatedAt")]
    public string? GeneratedAt { get; set; }

    [JsonPropertyName("applications")]
    public List<ApplicationDescriptor> Applications { get; set; } = [];

    /// <summary>
    /// Globale Sprachdialogeinstellungen als Standardwerte für alle Anwendungen.
    /// </summary>
    [JsonPropertyName("voiceDialogSettings")]
    public VoiceDialogSettings? VoiceDialogSettings { get; set; }

    /// <summary>
    /// Auswahldialog mit Ansagen, Intent-Lexikon und Zahlwörtern.
    /// Wird nicht in kanalspezifische Projektionen kopiert.
    /// </summary>
    [JsonPropertyName("selectionDialog")]
    public ApplicationSelectionDialog SelectionDialog { get; set; } = new();
}

