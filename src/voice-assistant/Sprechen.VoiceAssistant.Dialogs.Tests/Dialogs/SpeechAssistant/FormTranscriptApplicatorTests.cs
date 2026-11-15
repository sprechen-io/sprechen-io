using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormTranscriptApplicatorTests
{
    [Fact]
    public async Task TryApplyTranscript_WhenTranscriptBlank_DoesNotCaptureOrMutate()
    {
        var interpreter = new FormInterpreterRuntimeOwner(CreateRequiredContract(), "de", ImmutableDictionary<string, string>.Empty);

        var result = interpreter.TranscriptApplicationService.TryApplyTranscript(" ");

        Assert.False(result.ValueCaptured);
        Assert.Empty(result.Violations);
        Assert.Empty(await interpreter.GetValuesSnapshotAsync());
    }

    [Fact]
    public async Task TryApplyTranscript_WhenCaptureSucceeds_StoresCanonicalValue()
    {
        var interpreter = new FormInterpreterRuntimeOwner(CreateRequiredContract(), "de", ImmutableDictionary<string, string>.Empty);

        var result = interpreter.TranscriptApplicationService.TryApplyTranscript("Max");

        Assert.True(result.ValueCaptured);
        Assert.Empty(result.Violations);
        Assert.Equal("Max", (await interpreter.GetValuesSnapshotAsync())["name"]);
    }

    [Fact]
    public async Task TryApplyComposedValue_WhenTextField_CommitsComposedValue()
    {
        var interpreter = new FormInterpreterRuntimeOwner(
            CreateRequiredContract("""{"kind":"string"}"""),
            "de",
            ImmutableDictionary<string, string>.Empty);

        var result = interpreter.TranscriptApplicationService.TryApplyComposedValue("Max");

        Assert.True(result.ValueCaptured);
        Assert.Equal("Max", (await interpreter.GetValuesSnapshotAsync())["name"]);
    }

    [Fact]
    public async Task TryApplyComposedValue_WhenBooleanFieldAndNonBooleanValue_DoesNotCapture()
    {
        // Auch ein zusammengesetzter Buchstabier- oder Ziffernwert muss zum Feldtyp passen.
        // „vielleicht“ darf hier nicht als boolescher Wert gespeichert werden.
        var interpreter = new FormInterpreterRuntimeOwner(
            CreateRequiredContract("""{"kind":"boolean"}"""),
            "de",
            ImmutableDictionary<string, string>.Empty);

        var result = interpreter.TranscriptApplicationService.TryApplyComposedValue("vielleicht");

        Assert.False(result.ValueCaptured);
        Assert.Empty(await interpreter.GetValuesSnapshotAsync());
    }

    [Fact]
    public async Task TryApplyComposedValue_WhenNumberFieldAndNonNumericValue_DoesNotCapture()
    {
        var interpreter = new FormInterpreterRuntimeOwner(
            CreateRequiredContract("""{"kind":"integer"}"""),
            "de",
            ImmutableDictionary<string, string>.Empty);

        var result = interpreter.TranscriptApplicationService.TryApplyComposedValue("abc");

        Assert.False(result.ValueCaptured);
        Assert.Empty(await interpreter.GetValuesSnapshotAsync());
    }

    [Fact]
    public async Task TryApplyComposedValue_WhenDateFieldAndNonDateValue_DoesNotCapture()
    {
        var interpreter = new FormInterpreterRuntimeOwner(
            CreateRequiredContract("""{"kind":"date"}"""),
            "de",
            ImmutableDictionary<string, string>.Empty);

        var result = interpreter.TranscriptApplicationService.TryApplyComposedValue("bald");

        Assert.False(result.ValueCaptured);
        Assert.Empty(await interpreter.GetValuesSnapshotAsync());
    }

    private static FormContract CreateRequiredContract(string valueTypeJson = """{"kind":"string"}""")
    {
        var stringValueType = JsonDocument.Parse(valueTypeJson).RootElement;
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "test",
            FormId = FormId.From("test"),
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("root"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("name"),
                    Name = "name",
                    ValueType = stringValueType
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("nameField"),
                    VariableId = VariableId.From("name"),
                    Label = new LocalizedText { ["de"] = "Name" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("root"),
                    Title = new LocalizedText { ["de"] = "Root" },
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("nameField") }]
                }
            ]
        };
    }
}
