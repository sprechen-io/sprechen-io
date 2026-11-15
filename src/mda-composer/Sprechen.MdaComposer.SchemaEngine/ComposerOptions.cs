namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Konfiguriert den Schemakatalog des Composers, um das Evaluationsmodell bei Bedarf auszublenden.
/// </summary>
public sealed class ComposerOptions
{
    public const string SectionName = "Composer";

    /// <summary>
    /// Legt fest, ob der Katalog auch die Schemas des Evaluationsmodells enthält.
    /// </summary>
    public bool IncludeEvaluationSchemas { get; set; } = true;
}
