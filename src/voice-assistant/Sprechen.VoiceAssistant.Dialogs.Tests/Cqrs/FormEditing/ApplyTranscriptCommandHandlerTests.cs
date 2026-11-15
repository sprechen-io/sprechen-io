using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.Infrastructure;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormEditing;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormEditing;

public sealed class ApplyTranscriptCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ValidTranscript_CapturesValue()
    {
        var interpreter = FormDialogRuntimeTestFixture.CreateInterpreter();
        var handler = new ApplyTranscriptCommandHandler(interpreter.TranscriptApplicationService);

        var result = await handler.HandleAsync(new ApplyTranscriptCommand("Max"));

        Assert.True(result.ValueCaptured);
        Assert.Equal("Max", (await interpreter.GetValuesSnapshotAsync())["name"]);
    }

    [Fact]
    public async Task HandleAsync_EmptyTranscript_DoesNotCapture()
    {
        var interpreter = FormDialogRuntimeTestFixture.CreateInterpreter();
        var handler = new ApplyTranscriptCommandHandler(interpreter.TranscriptApplicationService);

        var result = await handler.HandleAsync(new ApplyTranscriptCommand(""));

        Assert.False(result.ValueCaptured);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public async Task HandleAsync_EmptyTranscript_OnOptionalSetField_CommitsTheCanonicalEmptySet(int? minSelected)
    {
        // Ohne positive Mindestauswahl darf die ausdrücklich leere Auswahl als Wert gespeichert werden.
        // Der Handler erhält dafür eine leere Zeichenkette; der Test prüft keinen gesprochenen Abschlussbefehl.
        var interpreter = CreateSetFieldInterpreter(minSelected);
        var handler = new ApplyTranscriptCommandHandler(interpreter.TranscriptApplicationService);

        var result = await handler.HandleAsync(new ApplyTranscriptCommand(""));

        Assert.True(result.ValueCaptured);
        Assert.Empty(result.Violations);
        Assert.Equal(string.Empty, (await interpreter.GetValuesSnapshotAsync())["channels"]);
    }

    [Fact]
    public async Task HandleAsync_EmptyTranscript_OnSetFieldWithRequiredMinimum_DoesNotCapture()
    {
        // Bei einer Mindestauswahl von eins darf eine leere Eingabe keinen Wert setzen.
        var interpreter = CreateSetFieldInterpreter(minSelected: 1);
        var handler = new ApplyTranscriptCommandHandler(interpreter.TranscriptApplicationService);

        var result = await handler.HandleAsync(new ApplyTranscriptCommand(""));

        Assert.False(result.ValueCaptured);
        Assert.False((await interpreter.GetValuesSnapshotAsync()).ContainsKey("channels"));
    }

    private static FormInterpreterRuntimeOwner CreateSetFieldInterpreter(int? minSelected)
    {
        var minSelectedJson = minSelected.HasValue ? $",\"minSelected\":{minSelected.Value}" : string.Empty;
        var setValueType = JsonDocument.Parse(
            "{\"kind\":\"set\",\"values\":[" +
            "{\"code\":\"email\",\"displayName\":{\"de\":\"E-Mail\"}}," +
            "{\"code\":\"sms\",\"displayName\":{\"de\":\"SMS\"}}]" +
            minSelectedJson + "}").RootElement;

        var contract = new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("form"),
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("channels"),
                    Name = "channels",
                    ValueType = setValueType
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("channelsField"),
                    VariableId = VariableId.From("channels"),
                    Label = new LocalizedText { ["de"] = "Kontaktkanäle" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Hauptbereich" },
                    Members =
                    [
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("channelsField") }
                    ]
                }
            ]
        };

        return new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary<string, string>.Empty);
    }
}
