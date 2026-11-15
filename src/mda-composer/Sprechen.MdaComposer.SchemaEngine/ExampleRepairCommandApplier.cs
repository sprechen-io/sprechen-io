using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Wendet Reparaturaktionen auf das Dokument an und trennt damit ihre Ausführung von der Planung.
/// </summary>
internal sealed class ExampleRepairCommandApplier
{
    private readonly SchemaDefaultDocumentFactory _factory = new();
    private readonly ExampleDocumentEditor _editor = new();

    public JsonNode? Apply(JsonNode? document, ExampleRepairAction action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return action.Kind switch
        {
            ExampleRepairActionKind.InitializeMissingRequiredValue => ResetNode(document, action),
            ExampleRepairActionKind.ResetNodeToSchemaDefault => ResetNode(document, action),
            ExampleRepairActionKind.ResetUnionToDefaultVariant => ResetNode(document, action),
            _ => document,
        };
    }

    private JsonNode? ResetNode(JsonNode? document, ExampleRepairAction action)
    {
        return _editor.SetValue(
            document,
            action.Pointer,
            _factory.CreateDefault(action.TargetSchema));
    }
}
