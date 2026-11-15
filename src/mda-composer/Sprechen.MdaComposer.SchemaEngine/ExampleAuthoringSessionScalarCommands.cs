using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Setzt einzelne Werte und Schema-Standardwerte über die Sitzungsverwaltung, damit Änderungen im Verlauf erfasst werden.
/// </summary>
public sealed class ExampleAuthoringSessionScalarCommands
{
    private readonly ExampleAuthoringSessionMutationOrchestrator _mutations;
    private readonly ExampleAuthoringDocumentWorkspace _workspace;

    internal ExampleAuthoringSessionScalarCommands(
        ExampleAuthoringSessionMutationOrchestrator mutations,
        ExampleAuthoringDocumentWorkspace workspace)
    {
        _mutations = mutations;
        _workspace = workspace;
    }

    public void InitializeValue(string pointer, SchemaNode schema)
    {
        _mutations.ApplyDocumentMutation(document => _workspace.SetValue(document, pointer, _workspace.CreateDefault(schema)));
    }

    public void SetString(string pointer, string value)
    {
        _mutations.ApplyDocumentMutation(document => _workspace.SetValue(document, pointer, JsonValue.Create(value)));
    }

    public void SetInteger(string pointer, int value)
    {
        _mutations.ApplyDocumentMutation(document => _workspace.SetValue(document, pointer, JsonValue.Create(value)));
    }

    public void SetNumber(string pointer, double value)
    {
        _mutations.ApplyDocumentMutation(document => _workspace.SetValue(document, pointer, JsonValue.Create(value)));
    }

    public void SetBoolean(string pointer, bool value)
    {
        _mutations.ApplyDocumentMutation(document => _workspace.SetValue(document, pointer, JsonValue.Create(value)));
    }

    public void SwitchUnionVariant(string pointer, SchemaNode optionSchema)
    {
        _mutations.ApplyDocumentMutation(document => _workspace.SetValue(document, pointer, _workspace.CreateDefault(optionSchema)));
    }
}
