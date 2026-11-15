using System.Collections.Immutable;
using System.Text.Json;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandStatusHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenSummaryContainsStoredAndSpokenValues_FormatsSummaryAtPromptingBoundary()
    {
        var speechService = new RecordingSpeechService();
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var handler = new FormVoiceCommandStatusHandler(telemetry.Object, speechService);
        var context = CreateContext(
            CreateSummaryContract(),
            ImmutableDictionary.Create<string, string>()
                .Add("name", "Max")
                .Add("pin", "1234"));

        await handler.HandleAsync(
            context with
            {
                Request = context.Request with { CommandId = GlobalCommandIds.Summary }
            },
            CancellationToken.None);

        var call = Assert.Single(speechService.Calls);
        Assert.Equal("Name ist Max. PIN: aus Modell gespeichert.", call.Text);
        Assert.Equal("de", call.IsoCode);
    }

    [Fact]
    public async Task HandleAsync_WhenSummaryIsEmpty_SpeaksEmptySummaryPrompt()
    {
        var speechService = new RecordingSpeechService();
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var handler = new FormVoiceCommandStatusHandler(telemetry.Object, speechService);
        var context = CreateContext(CreateSummaryContract(), ImmutableDictionary<string, string>.Empty);

        await handler.HandleAsync(
            context with
            {
                Request = context.Request with { CommandId = GlobalCommandIds.Summary }
            },
            CancellationToken.None);

        var call = Assert.Single(speechService.Calls);
        Assert.Equal(FormSummaryPromptBuilder.GetEmptySummaryPrompt(context.Contract, "de", PolitenessLevel.Formal), call.Text);
        Assert.Equal("de", call.IsoCode);
    }

    [Theory]
    [InlineData("de", "achter Januar neunzehnhundertachtzig", "ja")]
    [InlineData("en", "January 8, nineteen eighty", "yes")]
    [InlineData("fr", "8 janvier 1980", "oui")]
    [InlineData("es", "8 de enero de 1980", "sí")]
    [InlineData("it", "8 gennaio 1980", "sì")]
    public async Task HandleAsync_WhenSummaryHasDateAndBooleanValues_SpeaksLocalizedFormsNotRawCanonicalLiterals(
        string isoCode,
        string expectedSpokenDate,
        string expectedYesWord)
    {
        // Die kanonischen Werte werden im angeforderten Zusammenfassungstext als Datum und Ja-Wort der Sitzungssprache dargestellt.
        var speechService = new RecordingSpeechService();
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var handler = new FormVoiceCommandStatusHandler(telemetry.Object, speechService);
        var context = CreateContext(
            CreateTemporalSummaryContract(),
            ImmutableDictionary.Create<string, string>()
                .Add("birthDate", "1980-01-08")
                .Add("newsletter", "true"),
            isoCode);

        await handler.HandleAsync(
            context with
            {
                Request = context.Request with { CommandId = GlobalCommandIds.Summary }
            },
            CancellationToken.None);

        // Die erwartete Trennung zwischen Bezeichnung und Wert folgt der jeweiligen Sprache.
        var separator = isoCode switch { "fr" => " : ", "de" => " ist ", _ => ": " };
        var call = Assert.Single(speechService.Calls);
        Assert.Contains($"Geburtsdatum{separator}{expectedSpokenDate}", call.Text, StringComparison.Ordinal);
        Assert.Contains($"Newsletter{separator}{expectedYesWord}", call.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("1980-01-08", call.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("true", call.Text, StringComparison.Ordinal);
        Assert.Equal(isoCode, call.IsoCode);
    }

    private static FormVoiceCommandGlobalCommandContext CreateContext(
        FormContract contract,
        ImmutableDictionary<string, string> initialValues,
        string isoCode = "de")
    {
        var interpreter = new FormInterpreterRuntimeOwner(contract, requestedIsoCode: isoCode, initialValues);
        var form = new FormDescriptor
        {
            FormId = FormId.From("form"),
            DisplayName = new LocalizedText { ["de"] = "Formular" },
            Description = new LocalizedText { ["de"] = "Formular" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };

        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { ["de"] = "Anwendung" },
            Description = new LocalizedText { ["de"] = "Anwendung" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms = [form]
        };

        return new FormVoiceCommandGlobalCommandContext(
            Request: new FormVoiceCommandGlobalCommandRequest(
                CommandId: GlobalCommandIds.Summary,
                IsoCode: isoCode,
                Argument: null),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings(),
                Application: application,
                CompletedFormIds: ImmutableHashSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase),
                Form: form,
                Contract: contract),
            Runtime: new FormVoiceCommandGlobalCommandRuntime(
                Interpreter: interpreter,
                Session: new EvaluationSession("summary-session", EvaluationChannel.Voice, "test-app", "form-1"),
                StateTracker: new TestFormVoiceCommandDialogStateTracker()));
    }

    private static FormContract CreateTemporalSummaryContract()
    {
        var dateValueType = JsonDocument.Parse("""{"kind":"date"}""").RootElement;
        var booleanValueType = JsonDocument.Parse("""{"kind":"boolean"}""").RootElement;

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("form"),
            DisplayName = InAllLanguages("Formular"),
            SupportedLanguages = ["de", "en", "fr", "es", "it"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            EntryFieldGroupId = FieldGroupId.From("main"),
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("birthDate"),
                    Name = "birthDate",
                    ValueType = dateValueType
                },
                new VariableDefinition
                {
                    VariableId = VariableId.From("newsletter"),
                    Name = "newsletter",
                    ValueType = booleanValueType
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("birthDateField"),
                    VariableId = VariableId.From("birthDate"),
                    Label = InAllLanguages("Geburtsdatum"),
                    Editability = FieldEditability.UserEditable
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("newsletterField"),
                    VariableId = VariableId.From("newsletter"),
                    Label = InAllLanguages("Newsletter"),
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = InAllLanguages("Hauptteil"),
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("birthDateField") },
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("newsletterField") }
                    ]
                }
            ]
        };
    }

    // Gleiche Feldbezeichnungen in allen fünf Sprachen isolieren die Prüfung der lokalisierten Werte.
    private static LocalizedText InAllLanguages(string text)
        => new()
        {
            ["de"] = text,
            ["en"] = text,
            ["fr"] = text,
            ["es"] = text,
            ["it"] = text
        };

    private static FormContract CreateSummaryContract()
    {
        var stringValueType = JsonDocument.Parse("""{"kind":"string"}""").RootElement;

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("form"),
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
                    ValueType = stringValueType
                },
                new VariableDefinition
                {
                    VariableId = VariableId.From("pin"),
                    Name = "pin",
                    ValueType = stringValueType,
                    SensitiveDataPolicy = new SensitiveDataPolicy
                    {
                        ReadbackPolicy = SensitiveDataPolicyDefaults.ReadbackNever
                    }
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
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("pinField"),
                    VariableId = VariableId.From("pin"),
                    Label = new LocalizedText { ["de"] = "PIN" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Hauptteil" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("nameField") },
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("pinField") }
                    ]
                }
            ],
            Projections = new ContractProjections
            {
                Voice = new VoiceProjection
                {
                    Hints = new VoiceHintsDefinition
                    {
                        Prompts =
                        [
                            new VoicePrompt
                            {
                                PromptId = FormDialogPromptIds.SummaryStoredValue,
                                Text = new LocalizedText
                                {
                                    ["de"] = "{{fieldLabel}}: aus Modell gespeichert."
                                }
                            }
                        ]
                    }
                }
            }
        };
    }
}
