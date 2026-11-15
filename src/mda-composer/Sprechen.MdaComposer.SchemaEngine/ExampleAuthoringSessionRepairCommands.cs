using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Wendet vorgeschlagene Dokumentreparaturen über die Sitzungsverwaltung an, damit sie im Verlauf erfasst werden.
/// </summary>
public sealed class ExampleAuthoringSessionRepairCommands
{
    private const int MaxRepairIterations = 50;

    private readonly ExampleAuthoringSessionMutationOrchestrator _mutations;
    private readonly ExampleAuthoringDocumentWorkspace _workspace;
    private readonly ExampleRepairPlanner _repairPlanner;
    private readonly ExampleRepairCommandApplier _applier = new();

    internal ExampleAuthoringSessionRepairCommands(
        ExampleAuthoringSessionMutationOrchestrator mutations,
        ExampleAuthoringDocumentWorkspace workspace,
        ExampleRepairPlanner repairPlanner)
    {
        _mutations = mutations;
        _workspace = workspace;
        _repairPlanner = repairPlanner;
    }

    public bool ApplyRepairAction(string actionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);

        var action = _mutations.RepairActions.FirstOrDefault(candidate =>
            candidate.ActionId.Equals(actionId, StringComparison.Ordinal));
        if (action is null)
        {
            return false;
        }

        _mutations.ApplyDocumentMutation(document => _applier.Apply(document, action));
        return true;
    }

    public bool ApplyAllRepairActions()
    {
        if (_workspace.CurrentRootSchema is null || _mutations.RepairActions.Count == 0)
        {
            return false;
        }

        _mutations.ApplyDocumentMutation(ApplyRepairsUntilStable);
        return true;
    }

    private JsonNode? ApplyRepairsUntilStable(JsonNode? document)
    {
        var workingDocument = document;
        var iterationCount = 0;

        while (iterationCount < MaxRepairIterations)
        {
            if (_workspace.CurrentRootSchema is null)
            {
                break;
            }

            var repairActions = _repairPlanner.Build(_workspace.CurrentRootSchema, workingDocument);
            if (repairActions.Count == 0)
            {
                break;
            }

            workingDocument = _applier.Apply(workingDocument, repairActions[0]);
            iterationCount++;
        }

        return workingDocument;
    }
}
