using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Prüft den Entwurf und ermittelt Reparaturvorschläge, um Ladezustand und Exportfreigabe zu bestimmen.
/// </summary>
internal sealed class ExampleAuthoringSessionStateEvaluator
{
    private readonly CanonicalExportValidator _canonicalExportValidator;
    private readonly ISchemaTextProvider? _schemaTextProvider;
    private readonly ExampleRepairPlanner _repairPlanner;
    private readonly ExampleAuthoringSessionLoadStateResolver _loadStates = new();
    private readonly ExampleAuthoringSessionStatusMessageBuilder _statusMessages = new();

    public ExampleAuthoringSessionStateEvaluator(
        CanonicalExportValidator canonicalExportValidator,
        ISchemaTextProvider? schemaTextProvider,
        ExampleRepairPlanner repairPlanner)
    {
        _canonicalExportValidator = canonicalExportValidator;
        _schemaTextProvider = schemaTextProvider;
        _repairPlanner = repairPlanner;
    }

    public ExampleAuthoringSessionStateEvaluation Evaluate(
        ComposerRootSchemaInfo? rootSchemaInfo,
        SchemaNode? rootSchema,
        JsonNode? document,
        ExampleSessionDocumentSource documentSource,
        string currentSnapshot)
    {
        if (rootSchemaInfo is null || rootSchema is null || document is null)
        {
            return BuildNoDocumentEvaluation();
        }

        var repairActions = _repairPlanner.Build(rootSchema, document);
        var schemaTexts = _schemaTextProvider?.GetSchemaTexts();
        if (schemaTexts is null || schemaTexts.Count == 0)
        {
            return BuildUncheckedEvaluation(documentSource, repairActions);
        }

        return BuildCanonicalEvaluation(rootSchemaInfo.FileName, documentSource, currentSnapshot, schemaTexts, repairActions);
    }

    private static ExampleAuthoringSessionStateEvaluation BuildNoDocumentEvaluation()
    {
        return new ExampleAuthoringSessionStateEvaluation
        {
            LoadState = ExampleSessionLoadState.NoDocument,
            ExportState = ExampleSessionExportState.Unknown,
            StatusMessage = "Kein Dokument geladen."
        };
    }

    private ExampleAuthoringSessionStateEvaluation BuildUncheckedEvaluation(
        ExampleSessionDocumentSource documentSource,
        IReadOnlyList<ExampleRepairAction> repairActions)
    {
        return new ExampleAuthoringSessionStateEvaluation
        {
            RepairActions = repairActions,
            ExportState = ExampleSessionExportState.Unknown,
            LoadState = _loadStates.ResolveUnchecked(documentSource),
            StatusMessage = _statusMessages.Build(documentSource, null, repairActions, canonicalValidationAvailable: false)
        };
    }

    private ExampleAuthoringSessionStateEvaluation BuildCanonicalEvaluation(
        string schemaFileName,
        ExampleSessionDocumentSource documentSource,
        string currentSnapshot,
        IReadOnlyDictionary<string, string> schemaTexts,
        IReadOnlyList<ExampleRepairAction> repairActions)
    {
        var canonicalValidation = _canonicalExportValidator.Validate(
            schemaFileName,
            currentSnapshot,
            schemaTexts);

        return new ExampleAuthoringSessionStateEvaluation
        {
            RepairActions = repairActions,
            CanonicalValidation = canonicalValidation,
            ExportState = canonicalValidation.IsValid
                ? ExampleSessionExportState.Exportable
                : ExampleSessionExportState.BlockedByCanonicalValidation,
            LoadState = _loadStates.ResolveCanonical(documentSource, canonicalValidation),
            StatusMessage = _statusMessages.Build(documentSource, canonicalValidation, repairActions, canonicalValidationAvailable: true)
        };
    }
}
