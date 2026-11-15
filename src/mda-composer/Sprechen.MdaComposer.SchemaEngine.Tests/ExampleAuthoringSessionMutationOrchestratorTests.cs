using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class ExampleAuthoringSessionMutationOrchestratorTests
{
    [Fact]
    public void SelectRootSchema_CreatesCleanDocumentAndNotifies()
    {
        var workspace = new ExampleAuthoringDocumentWorkspace();
        var notificationCount = 0;
        var dispatcher = CreateDispatcher(workspace, () => notificationCount++);

        dispatcher.SelectRootSchema(CreateRootInfo(), CreateRootSchema());

        Assert.False(dispatcher.IsDirty);
        Assert.False(dispatcher.CanUndo);
        Assert.Equal(ExampleSessionLoadState.SchemaDraft, dispatcher.LoadState);
        Assert.Equal(ExampleSessionExportState.Unknown, dispatcher.ExportState);
        Assert.Equal(1, notificationCount);
        Assert.Equal(string.Empty, workspace.GetNode("/title")?.ToString());
    }

    [Fact]
    public void ApplyDocumentMutation_RecordsHistoryAndDirtyState()
    {
        var workspace = new ExampleAuthoringDocumentWorkspace();
        var notificationCount = 0;
        var dispatcher = CreateDispatcher(workspace, () => notificationCount++);
        dispatcher.SelectRootSchema(CreateRootInfo(), CreateRootSchema());

        dispatcher.ApplyDocumentMutation(document => workspace.SetValue(document, "/title", JsonValue.Create("changed")));

        Assert.True(dispatcher.IsDirty);
        Assert.True(dispatcher.CanUndo);
        Assert.Equal("changed", workspace.GetNode("/title")?.ToString());
        Assert.Equal(2, notificationCount);

        dispatcher.Undo();

        Assert.Equal(string.Empty, workspace.GetNode("/title")?.ToString());
        Assert.False(dispatcher.IsDirty);
        Assert.True(dispatcher.CanRedo);
        Assert.Equal(3, notificationCount);
    }

    [Fact]
    public void ApplyDocumentMutation_WhenSnapshotIsUnchanged_DoesNotNotify()
    {
        var workspace = new ExampleAuthoringDocumentWorkspace();
        var notificationCount = 0;
        var dispatcher = CreateDispatcher(workspace, () => notificationCount++);
        dispatcher.SelectRootSchema(CreateRootInfo(), CreateRootSchema());

        dispatcher.ApplyDocumentMutation(document => document);

        Assert.False(dispatcher.IsDirty);
        Assert.False(dispatcher.CanUndo);
        Assert.Equal(1, notificationCount);
    }

    private static ExampleAuthoringSessionMutationOrchestrator CreateDispatcher(
        ExampleAuthoringDocumentWorkspace workspace,
        Action stateChanged)
    {
        return new ExampleAuthoringSessionMutationOrchestrator(
            workspace,
            new ExampleAuthoringSessionStateEvaluator(
                new CanonicalExportValidator(),
                schemaTextProvider: null,
                new ExampleRepairPlanner()),
            stateChanged);
    }

    private static ComposerRootSchemaInfo CreateRootInfo()
        => new("test.schema.json", "Test Root", "Tests", "Test root schema");

    private static SchemaNode CreateRootSchema()
        => new()
        {
            Name = "(root)",
            Type = SchemaNodeType.Object,
            Properties =
            [
                new SchemaNode
                {
                    Name = "title",
                    Type = SchemaNodeType.String,
                    IsRequired = true,
                },
            ],
        };
}
