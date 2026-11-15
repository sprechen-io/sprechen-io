using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandAmbiguitySignalEligibilityTests
{
    private readonly FormVoiceCommandAmbiguitySignalEligibility _eligibility = new();

    [Theory]
    // Die aufgeführten Freitextvarianten dürfen keine zusätzliche Rückfrage
    // zur Unterscheidung von Befehl und Wert freigeben.
    [InlineData(GlobalCommandIds.Next, "weiter", "weiter")]
    [InlineData(GlobalCommandIds.Help, "hilfe", "hilfe")]
    [InlineData(GlobalCommandIds.Cancel, "abbrechen", "abbrechen")]
    [InlineData(GlobalCommandIds.Cancel, "ich moechte das hier abbrechen und neu", "ich moechte das hier abbrechen und neu")]
    [InlineData(GlobalCommandIds.BackToFormSelection, "zurück zur formularauswahl", "zurück zur formularauswahl")]
    [InlineData(GlobalCommandIds.ChangeLanguage, "auf englisch wechseln", "auf englisch wechseln")]
    [InlineData(GlobalCommandIds.Repeat, "meine adresse ist wiederholen strasse 1", "meine adresse ist wiederholen strasse 1")]
    public void IsEligible_ForAnyFreeTextCommandCandidate_ReturnsFalse(
        string parsedCommandId,
        string trimmedTranscript,
        string inputTranscript)
    {
        var context = CreateContext();
        var interpretation = CreateInterpretation(parsedCommandId, trimmedTranscript, inputTranscript);

        var eligible = _eligibility.IsEligible(context, interpretation);

        Assert.False(eligible);
    }

    private static FormVoiceCommandTranscriptInterpretation CreateInterpretation(
        string parsedCommandId,
        string trimmedTranscript,
        string inputTranscript)
        => new(
            new FormVoiceCommandTranscriptSource(
                trimmedTranscript,
                inputTranscript,
                false),
            new FormVoiceCommandParsedCommand(
                true,
                parsedCommandId,
                null,
                CommandSignalClass.Ambiguous),
            new FormVoiceCommandInterpretationMode(
                true,
                false,
                false,
                false,
                false));

    private static FormVoiceCommandContext CreateContext()
    {
        var contract = new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Formular" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            EntryFieldGroupId = FieldGroupId.From("main"),
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("name"),
                    Name = "name",
                    ValueType = JsonDocument.Parse("""{"kind":"string"}""").RootElement
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("name"),
                    VariableId = VariableId.From("name"),
                    Label = new LocalizedText { ["de"] = "Name" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members =
                    [
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("name") }
                    ]
                }
            ]
        };

        return new FormVoiceCommandContext(
            SessionIdentity: new FormVoiceCommandSessionIdentity(
                IsoCode: "de",
                ApplicationId: "app",
                FormId: "current",
                WakeWordActivationForms: []),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings(),
                Application: new ApplicationDescriptor
                {
                    ApplicationId = "app",
                    DisplayName = new LocalizedText { ["de"] = "App" },
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("current"),
                            DisplayName = new LocalizedText { ["de"] = "Formular" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        }
                    ]
                },
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: new FormDescriptor
                {
                    FormId = FormId.From("current"),
                    DisplayName = new LocalizedText { ["de"] = "Formular" },
                    LatestContractVersion = "1",
                    AvailableContractVersions = ["1"]
                },
                Contract: contract),
            Values: new FormVoiceCommandValueSnapshot(
                ValuesByVariableId: ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                SharedAdoptions: []));
    }
}
