using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Koordiniert Dokumentänderungen, Verlauf und erneute Validierung, damit Sitzungszustand und Oberfläche aktuell bleiben.
/// </summary>
internal sealed class ExampleAuthoringSessionMutationOrchestrator
{
    private readonly ExampleAuthoringDocumentWorkspace _workspace;
    private readonly ExampleAuthoringSessionHistory _history = new();
    private readonly ExampleAuthoringSessionStateEvaluator _stateEvaluator;
    private readonly Action _stateChanged;
    private string _cleanSnapshot = "{}";

    public ExampleAuthoringSessionMutationOrchestrator(
        ExampleAuthoringDocumentWorkspace workspace,
        ExampleAuthoringSessionStateEvaluator stateEvaluator,
        Action stateChanged)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _stateEvaluator = stateEvaluator ?? throw new ArgumentNullException(nameof(stateEvaluator));
        _stateChanged = stateChanged ?? throw new ArgumentNullException(nameof(stateChanged));
        StatusMessage = "Kein Dokument geladen.";
        RepairActions = [];
    }

    public ExampleSessionLoadState LoadState { get; private set; } = ExampleSessionLoadState.NoDocument;

    public ExampleSessionExportState ExportState { get; private set; } = ExampleSessionExportState.Unknown;

    public CanonicalValidationResult? CanonicalValidation { get; private set; }

    public IReadOnlyList<ExampleRepairAction> RepairActions { get; private set; }

    public string StatusMessage { get; private set; }

    public bool CanUndo => _history.CanUndo;

    public bool CanRedo => _history.CanRedo;

    public bool IsDirty { get; private set; }

    public void SelectRootSchema(ComposerRootSchemaInfo info, SchemaNode schema)
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(schema);

        _workspace.SelectRootSchema(info, schema);
        ResetHistory(markCurrentAsClean: true);
    }

    public void LoadDocument(string json, ExampleSessionDocumentSource source)
    {
        var loadedDocument = ExampleAuthoringDocumentWorkspace.Parse(json);
        _workspace.SetDocumentSource(source);
        ApplyReplacement(loadedDocument, markCurrentAsClean: true);
    }

    public void NewDocument()
    {
        if (_workspace.TryCreateNewDefaultDocument(out var document))
        {
            ApplyReplacement(document, markCurrentAsClean: true);
        }
    }

    public void MarkClean()
    {
        _cleanSnapshot = GetJson();
        IsDirty = false;
        NotifyStateChanged();
    }

    public void Undo()
    {
        if (_history.TryUndo(GetJson(), out var previousSnapshot))
        {
            RestoreHistorySnapshot(previousSnapshot);
        }
    }

    public void Redo()
    {
        if (_history.TryRedo(GetJson(), out var nextSnapshot))
        {
            RestoreHistorySnapshot(nextSnapshot);
        }
    }

    public string GetJson() => _workspace.GetJson();

    public void ApplyDocumentMutation(Func<JsonNode?, JsonNode?> mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);

        var before = GetJson();
        var updatedDocument = mutation(_workspace.Document);
        var after = ExampleAuthoringDocumentWorkspace.Serialize(updatedDocument);
        if (!_history.TryRecordMutation(before, after))
        {
            return;
        }

        _workspace.ReplaceDocument(updatedDocument);
        ReevaluateDocumentState(after);
        UpdateDirty(after);
        NotifyStateChanged();
    }

    private void ApplyReplacement(JsonNode? replacement, bool markCurrentAsClean)
    {
        var before = GetJson();
        var after = ExampleAuthoringDocumentWorkspace.Serialize(replacement);
        _history.RecordReplacement(before, after);
        _workspace.ReplaceDocument(replacement);

        ReevaluateDocumentState(after);

        if (markCurrentAsClean)
        {
            _cleanSnapshot = after;
            IsDirty = false;
        }
        else
        {
            UpdateDirty(after);
        }

        NotifyStateChanged();
    }

    private void ResetHistory(bool markCurrentAsClean)
    {
        _history.Reset();

        var currentSnapshot = GetJson();
        ReevaluateDocumentState(currentSnapshot);

        if (markCurrentAsClean)
        {
            _cleanSnapshot = currentSnapshot;
            IsDirty = false;
        }
        else
        {
            UpdateDirty(currentSnapshot);
        }

        NotifyStateChanged();
    }

    private void ReevaluateDocumentState(string currentSnapshot)
    {
        var evaluation = _stateEvaluator.Evaluate(
            _workspace.CurrentRootSchemaInfo,
            _workspace.CurrentRootSchema,
            _workspace.Document,
            _workspace.DocumentSource,
            currentSnapshot);

        LoadState = evaluation.LoadState;
        ExportState = evaluation.ExportState;
        CanonicalValidation = evaluation.CanonicalValidation;
        RepairActions = evaluation.RepairActions;
        StatusMessage = evaluation.StatusMessage;
    }

    private void UpdateDirty(string currentSnapshot)
    {
        IsDirty = !string.Equals(currentSnapshot, _cleanSnapshot, StringComparison.Ordinal);
    }

    private void RestoreHistorySnapshot(string snapshot)
    {
        _workspace.ReplaceDocument(ExampleAuthoringDocumentWorkspace.Parse(snapshot));
        ReevaluateDocumentState(snapshot);
        UpdateDirty(snapshot);
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        _stateChanged();
    }
}
