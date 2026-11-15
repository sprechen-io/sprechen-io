namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Bündelt Ladezustand, Exportfreigabe, Validierung und Reparaturvorschläge für die Statusanzeige.
/// </summary>
internal sealed class ExampleAuthoringSessionStateEvaluation
{
    public ExampleSessionLoadState LoadState { get; init; }

    public ExampleSessionExportState ExportState { get; init; }

    public CanonicalValidationResult? CanonicalValidation { get; init; }

    public IReadOnlyList<ExampleRepairAction> RepairActions { get; init; } = Array.Empty<ExampleRepairAction>();

    public string StatusMessage { get; init; } = string.Empty;
}
