namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Ändert Arrayeinträge über die Sitzungsverwaltung, damit Einfügen, Löschen und Verschieben im Verlauf erfasst werden.
/// </summary>
public sealed class ExampleAuthoringSessionArrayCommands
{
    private readonly ExampleAuthoringSessionMutationOrchestrator _mutations;
    private readonly ExampleAuthoringDocumentWorkspace _workspace;

    internal ExampleAuthoringSessionArrayCommands(
        ExampleAuthoringSessionMutationOrchestrator mutations,
        ExampleAuthoringDocumentWorkspace workspace)
    {
        _mutations = mutations;
        _workspace = workspace;
    }

    public void AddArrayItem(string arrayPointer, SchemaNode itemSchema)
    {
        _mutations.ApplyDocumentMutation(document =>
            _workspace.AddArrayItem(document, arrayPointer, _workspace.CreateDefault(itemSchema)));
    }

    public void RemoveArrayItem(string arrayPointer, int index)
    {
        _mutations.ApplyDocumentMutation(document => _workspace.RemoveArrayItem(document, arrayPointer, index));
    }

    public void MoveArrayItem(string arrayPointer, int index, int direction)
    {
        _mutations.ApplyDocumentMutation(document => _workspace.MoveArrayItem(document, arrayPointer, index, direction));
    }
}
