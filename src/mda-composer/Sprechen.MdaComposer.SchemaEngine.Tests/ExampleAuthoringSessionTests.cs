using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class ExampleAuthoringSessionTests
{
    [Fact]
    public void SelectRootSchema_CreatesDefaultDocument_AndStartsAsExportableSchemaDraft()
    {
        var session = CreateSession("test.schema.json", CreateSimpleSchemaText());

        session.SelectRootSchema(CreateRootInfo(), CreateRootSchema());

        Assert.NotNull(session.CurrentRootSchema);
        Assert.NotNull(session.Document);
        Assert.False(session.IsDirty);
        Assert.False(session.CanUndo);
        Assert.Equal(ExampleSessionDocumentSource.SchemaDefault, session.DocumentSource);
        Assert.Equal(ExampleSessionLoadState.SchemaDraft, session.LoadState);
        Assert.Equal(ExampleSessionExportState.Exportable, session.ExportState);
        Assert.True(session.CanExport);
        Assert.Empty(session.RepairActions);
        Assert.Equal(string.Empty, session.Queries.GetNode("/title")?.ToString());
    }

    [Fact]
    public void ImportJsonDocument_WithMissingRequiredValue_ProducesRepairAction_AndBecomesExportableAfterRepair()
    {
        var session = CreateSession("test.schema.json", CreateSimpleSchemaText());
        session.SelectRootSchema(CreateRootInfo(), CreateRootSchema());

        session.ImportJsonDocument("""{"notes":"nur optional"}""");

        Assert.Equal(ExampleSessionDocumentSource.JsonImport, session.DocumentSource);
        Assert.Equal(ExampleSessionLoadState.LoadedInvalidButParsable, session.LoadState);
        Assert.Equal(ExampleSessionExportState.BlockedByCanonicalValidation, session.ExportState);
        Assert.False(session.CanExport);
        Assert.Single(session.RepairActions);

        var repairAction = session.RepairActions[0];
        Assert.Equal(ExampleRepairActionKind.InitializeMissingRequiredValue, repairAction.Kind);
        Assert.Equal("/title", repairAction.Pointer);

        Assert.True(session.Repairs.ApplyRepairAction(repairAction.ActionId));
        Assert.Equal(string.Empty, session.Queries.GetNode("/title")?.ToString());
        Assert.Equal(ExampleSessionLoadState.LoadedValid, session.LoadState);
        Assert.True(session.CanExport);
        Assert.Empty(session.RepairActions);
    }

    [Fact]
    public void ImportJsonDocument_WithWrongNodeType_ProducesResetRepairAction()
    {
        var session = CreateSession("nested.schema.json", CreateNestedSchemaText());
        session.SelectRootSchema(CreateNestedRootInfo(), CreateNestedRootSchema());

        session.ImportJsonDocument("""{"title":"ok","metadata":"wrong-type"}""");

        Assert.Equal(ExampleSessionLoadState.LoadedInvalidButParsable, session.LoadState);
        Assert.Contains(
            session.RepairActions,
            action => action.Kind == ExampleRepairActionKind.ResetNodeToSchemaDefault
                && action.Pointer == "/metadata");

        session.Repairs.ApplyAllRepairActions();

        Assert.IsType<JsonObject>(session.Queries.GetNode("/metadata"));
        Assert.True(session.CanExport);
        Assert.Empty(session.RepairActions);
    }

    [Fact]
    public void ImportJsonDocument_WithInvalidUnionVariant_ProducesUnionRepairAction()
    {
        var session = CreateSession("union.schema.json", CreateUnionSchemaText());
        session.SelectRootSchema(CreateUnionRootInfo(), CreateUnionRootSchema());

        session.ImportJsonDocument("""{"title":"ok","mode":true}""");

        var repairAction = Assert.Single(session.RepairActions);
        Assert.Equal(ExampleRepairActionKind.ResetUnionToDefaultVariant, repairAction.Kind);
        Assert.Equal("/mode", repairAction.Pointer);

        session.Repairs.ApplyRepairAction(repairAction.ActionId);

        Assert.Equal("auto", session.Queries.GetNode("/mode")?.ToString());
        Assert.True(session.CanExport);
    }

    [Fact]
    public void AddOptionalProperty_IsOwnedBySessionHistory()
    {
        var session = CreateSession("test.schema.json", CreateSimpleSchemaText());
        var schema = CreateRootSchema();

        session.SelectRootSchema(CreateRootInfo(), schema);
        var notesProperty = schema.Properties!.Single(property => property.Name == "notes");

        session.Objects.AddObjectProperty(JsonPointer.Root, notesProperty.Name, notesProperty);

        Assert.Equal(string.Empty, session.Queries.GetNode("/notes")?.ToString());
        Assert.True(session.IsDirty);
        Assert.True(session.CanUndo);

        session.Undo();
        Assert.Null(session.Queries.GetNode("/notes"));

        session.Redo();
        Assert.Equal(string.Empty, session.Queries.GetNode("/notes")?.ToString());
    }

    private static ExampleAuthoringSession CreateSession(string schemaFileName, string schemaText)
        => new(
            new CanonicalExportValidator(),
            new InMemorySchemaTextProvider(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [schemaFileName] = schemaText,
                }));

    private static ComposerRootSchemaInfo CreateRootInfo()
        => new("test.schema.json", "Test Root", "Tests", "Test root schema");

    private static ComposerRootSchemaInfo CreateNestedRootInfo()
        => new("nested.schema.json", "Nested Root", "Tests", "Nested root schema");

    private static ComposerRootSchemaInfo CreateUnionRootInfo()
        => new("union.schema.json", "Union Root", "Tests", "Union root schema");

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
                new SchemaNode
                {
                    Name = "notes",
                    Type = SchemaNodeType.String,
                    IsRequired = false,
                },
            ],
        };

    private static SchemaNode CreateNestedRootSchema()
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
                new SchemaNode
                {
                    Name = "metadata",
                    Type = SchemaNodeType.Object,
                    Properties =
                    [
                        new SchemaNode
                        {
                            Name = "locale",
                            Type = SchemaNodeType.String,
                            IsRequired = true,
                        },
                    ],
                    IsRequired = false,
                },
            ],
        };

    private static SchemaNode CreateUnionRootSchema()
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
                new SchemaNode
                {
                    Name = "mode",
                    Type = SchemaNodeType.OneOf,
                    IsRequired = true,
                    OneOf =
                    [
                        new SchemaNode
                        {
                            Name = "mode",
                            Type = SchemaNodeType.String,
                            EnumValues = ["auto", "manual"],
                        },
                        new SchemaNode
                        {
                            Name = "mode",
                            Type = SchemaNodeType.Integer,
                        },
                    ],
                },
            ],
        };

    private static string CreateSimpleSchemaText()
        => """
           {
             "$schema": "https://json-schema.org/draft/2020-12/schema",
             "$id": "https://Sprechen/schemas/test.schema.json",
             "type": "object",
             "required": ["title"],
             "properties": {
               "title": { "type": "string" },
               "notes": { "type": "string" }
             }
           }
           """;

    private static string CreateNestedSchemaText()
        => """
           {
             "$schema": "https://json-schema.org/draft/2020-12/schema",
             "$id": "https://Sprechen/schemas/nested.schema.json",
             "type": "object",
             "required": ["title"],
             "properties": {
               "title": { "type": "string" },
               "metadata": {
                 "type": "object",
                 "required": ["locale"],
                 "properties": {
                   "locale": { "type": "string" }
                 }
               }
             }
           }
           """;

    private static string CreateUnionSchemaText()
        => """
           {
             "$schema": "https://json-schema.org/draft/2020-12/schema",
             "$id": "https://Sprechen/schemas/union.schema.json",
             "type": "object",
             "required": ["title", "mode"],
             "properties": {
               "title": { "type": "string" },
               "mode": {
                 "oneOf": [
                   { "type": "string", "enum": ["auto", "manual"] },
                   { "type": "integer" }
                 ]
               }
             }
           }
           """;
}
