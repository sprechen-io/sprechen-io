using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormEditing;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Zusammenfassung einer ausdrücklich leer abgeschlossenen optionalen
/// Mehrfachauswahl. Ein gespeicherter leerer Wert erhält einen Eintrag mit dem
/// lokalisierten Wort für keine Auswahl; ein fehlender Wert bleibt ausgeschlossen.
/// Der Unterschied liegt im vorhandenen oder fehlenden Schlüssel.
/// </summary>
public sealed class FormSummaryEmptySetProjectionTests
{
    [Theory]
    [InlineData("de", "keine")]
    [InlineData("en", "none")]
    [InlineData("fr", "aucune")]
    [InlineData("es", "ninguna")]
    [InlineData("it", "nessuna")]
    public async Task BuildSummaryItems_ExplicitlyEmptyOptionalSet_SpeaksLocalizedEmptySelectionWord(
        string languageCode,
        string expectedEmptyWord)
    {
        // Der leere Text wird über die Transkriptanwendung als ausdrückliche leere
        // Antwort übernommen.
        var interpreter = CreateOptionalSetInterpreter(languageCode);
        var handler = new ApplyTranscriptCommandHandler(interpreter.TranscriptApplicationService);
        var applyResult = await handler.HandleAsync(new ApplyTranscriptCommand(string.Empty));
        Assert.True(applyResult.ValueCaptured);

        var items = await interpreter.BuildSummaryItemsAsync();


        var item = Assert.Single(items);
        Assert.Equal("channelsField", item.FieldId);
        Assert.Equal(FormSpokenValueProjectionStatus.Empty, item.Status);
        Assert.Equal(expectedEmptyWord, item.SpokenValue);
    }

    [Fact]
    public async Task BuildSummaryItems_UnansweredOptionalSet_StaysOmittedFromTheSummary()
    {
        // Ohne angewendete Eingabe fehlt der Wertschlüssel für die Auswahlvariable.
        var interpreter = CreateOptionalSetInterpreter("de");

        var items = await interpreter.BuildSummaryItemsAsync();


        Assert.Empty(items);
    }

    private static FormInterpreterRuntimeOwner CreateOptionalSetInterpreter(string languageCode)
    {
        var setValueType = JsonDocument.Parse(
            "{\"kind\":\"set\",\"values\":[" +
            "{\"code\":\"email\",\"displayName\":{\"" + languageCode + "\":\"E-Mail\"}}," +
            "{\"code\":\"sms\",\"displayName\":{\"" + languageCode + "\":\"SMS\"}}]}").RootElement;

        var contract = new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("form"),
            SupportedLanguages = [languageCode],
            DefaultLanguage = languageCode,
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
                    Label = new LocalizedText { [languageCode] = "Kanäle" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { [languageCode] = "Hauptbereich" },
                    Members =
                    [
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("channelsField") }
                    ]
                }
            ]
        };

        return new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: languageCode,
            ImmutableDictionary<string, string>.Empty);
    }
}
