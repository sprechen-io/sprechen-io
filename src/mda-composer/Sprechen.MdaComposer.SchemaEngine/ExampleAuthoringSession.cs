using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Bündelt Laden, Bearbeiten und Reparieren von Beispieldokumenten für die Oberfläche des Composers.
/// </summary>
public sealed class ExampleAuthoringSession
{
    private readonly ExampleAuthoringDocumentWorkspace _workspace = new();
    private readonly ExampleRepairPlanner _repairPlanner = new();
    private readonly ExampleAuthoringSessionMutationOrchestrator _mutations;

    public ExampleAuthoringSession()
        : this(new CanonicalExportValidator(), null)
    {
    }

    public ExampleAuthoringSession(
        CanonicalExportValidator canonicalExportValidator,
        ISchemaTextProvider? schemaTextProvider)
    {
        var stateEvaluator = new ExampleAuthoringSessionStateEvaluator(
            canonicalExportValidator,
            schemaTextProvider,
            _repairPlanner);
        _mutations = new ExampleAuthoringSessionMutationOrchestrator(
            _workspace,
            stateEvaluator,
            NotifyStateChanged);
        Queries = new ExampleAuthoringSessionQueries(_workspace);
        Scalars = new ExampleAuthoringSessionScalarCommands(_mutations, _workspace);
        Objects = new ExampleAuthoringSessionObjectCommands(_mutations, _workspace);
        Arrays = new ExampleAuthoringSessionArrayCommands(_mutations, _workspace);
        Repairs = new ExampleAuthoringSessionRepairCommands(_mutations, _workspace, _repairPlanner);
    }

    public ComposerRootSchemaInfo? CurrentRootSchemaInfo => _workspace.CurrentRootSchemaInfo;
    public SchemaNode? CurrentRootSchema => _workspace.CurrentRootSchema;
    public JsonNode? Document => _workspace.Document;
    public ExampleSessionDocumentSource DocumentSource => _workspace.DocumentSource;
    public ExampleSessionLoadState LoadState => _mutations.LoadState;
    public ExampleSessionExportState ExportState => _mutations.ExportState;
    public CanonicalValidationResult? CanonicalValidation => _mutations.CanonicalValidation;
    public IReadOnlyList<ExampleRepairAction> RepairActions => _mutations.RepairActions;
    public string StatusMessage => _mutations.StatusMessage;
    public bool CanUndo => _mutations.CanUndo;
    public bool CanRedo => _mutations.CanRedo;
    public bool IsDirty => _mutations.IsDirty;
    public bool CanExport => ExportState == ExampleSessionExportState.Exportable;
    public ExampleAuthoringSessionQueries Queries { get; }
    public ExampleAuthoringSessionScalarCommands Scalars { get; }
    public ExampleAuthoringSessionObjectCommands Objects { get; }
    public ExampleAuthoringSessionArrayCommands Arrays { get; }
    public ExampleAuthoringSessionRepairCommands Repairs { get; }

    public event Action? StateChanged;

    public void SelectRootSchema(ComposerRootSchemaInfo info, SchemaNode schema)
    {
        _mutations.SelectRootSchema(info, schema);
    }

    public void LoadReferenceContract(string json)
    {
        LoadDocument(json, ExampleSessionDocumentSource.ReferenceContract);
    }

    public void ImportJsonDocument(string json)
    {
        LoadDocument(json, ExampleSessionDocumentSource.JsonImport);
    }

    public void RestoreDocument(string json)
    {
        LoadDocument(json, ExampleSessionDocumentSource.SessionRestore);
    }

    public void NewDocument()
    {
        _mutations.NewDocument();
    }

    public void MarkClean()
    {
        _mutations.MarkClean();
    }

    public void Undo()
    {
        _mutations.Undo();
    }

    public void Redo()
    {
        _mutations.Redo();
    }

    public string GetJson() => _mutations.GetJson();

    private void LoadDocument(string json, ExampleSessionDocumentSource source)
    {
        _mutations.LoadDocument(json, source);
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }

}
