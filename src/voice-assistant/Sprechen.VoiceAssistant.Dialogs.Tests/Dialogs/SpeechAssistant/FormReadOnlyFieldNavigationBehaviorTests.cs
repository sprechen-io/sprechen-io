using System.Collections.Immutable;
using System.Text.Json;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Navigation und Sprungbefehle für schreibgeschützte Anzeigefelder.
/// Eingabefokus, vorbereitete Vorlesetexte und Aufrufe der Fehlertelemetrie werden
/// mit simulierten Diensten geprüft, einschließlich leerer und vertraulicher Inhalte.
/// Der simulierte Sprachdienst protokolliert Ausgabeaufrufe und lehnt Aufnahmeversuche ab.
/// </summary>
public sealed class FormReadOnlyFieldNavigationBehaviorTests
{
    private const string InstructionText = "Die Referenznummer steht oben rechts auf der Rechnung.";
    private const string SensitiveText = "Interner Vorgang 12345678";
    private const string SecretText = "streng-geheimer-inhalt";

    [Fact]
    public async Task NextBackAndFocusRepair_NeverRestOnReadOnlyField()
    {
        var interpreter = CreateInterpreter();

        // Der erste Eingabefokus liegt auf einem bearbeitbaren Feld.
        Assert.Equal("refField", (await interpreter.GetFocusedFieldAsync()).FieldId);

        // Vorwärtsnavigation überspringt das schreibgeschützte Hinweisfeld.
        Assert.True(await interpreter.TryMoveFocusNextAsync());
        Assert.Equal("cityField", (await interpreter.GetFocusedFieldAsync()).FieldId);

        // Rückwärtsnavigation überspringt das Hinweisfeld ebenfalls.
        Assert.True(await interpreter.TryMoveFocusBackAsync());
        Assert.Equal("refField", (await interpreter.GetFocusedFieldAsync()).FieldId);

        // Ein absichtlich ungültig gesetzter Fokus auf dem Hinweisfeld wird
        // auf ein bearbeitbares Feld verschoben.
        interpreter.MutableState.SetFocusedField(FieldId.From("instructionField"));
        interpreter.Services.Navigator.EnsureFocusIsValid();
        Assert.Equal("cityField", (await interpreter.GetFocusedFieldAsync()).FieldId);
    }

    [Fact]
    public async Task JumpToReadOnlyField_SpeaksLabelAndValue_KeepsInputFocus_AndFiresNoNavigationError()
    {
        // Ein Sprung zum Hinweisfeld liefert einen Vorlesetext, ohne den Eingabefokus
        // zu verändern oder einen Navigationsfehler zu melden. Der simulierte Sprachdienst
        // würde jeden Aufnahmeversuch als Fehler zurückweisen.
        var speechService = new RecordingSpeechService();
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var handler = new FormVoiceCommandJumpHandler(telemetry.Object, speechService);
        var interpreter = CreateInterpreter();
        var context = CreateJumpContext(interpreter, argument: "Hinweis");

        Assert.Equal("refField", (await interpreter.GetFocusedFieldAsync()).FieldId);

        await handler.HandleAsync(context, CancellationToken.None);


        Assert.Equal("refField", (await interpreter.GetFocusedFieldAsync()).FieldId);


        telemetry.Verify(
            t => t.TrackNavigationError(It.IsAny<EvaluationSession>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);

        // Der einzige Ausgabeaufruf enthält Bezeichnung und Wert ohne Positionsangabe.
        var call = Assert.Single(speechService.Calls);
        Assert.Equal($"Hinweis ist {InstructionText}", call.Text);
    }

    [Fact]
    public async Task JumpToEditableFieldLabel_StillFocuses_WithoutReadbackOrTelemetry()
    {
        // Ein Sprung zu einem Eingabefeld verschiebt den Fokus. Der Sprunghandler
        // gibt dabei keinen Text aus und meldet keinen Navigationsfehler.
        var speechService = new RecordingSpeechService();
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var handler = new FormVoiceCommandJumpHandler(telemetry.Object, speechService);
        var interpreter = CreateInterpreter();
        var context = CreateJumpContext(interpreter, argument: "Stadt");

        await handler.HandleAsync(context, CancellationToken.None);

        Assert.Equal("cityField", (await interpreter.GetFocusedFieldAsync()).FieldId);
        Assert.Empty(speechService.Calls);
        telemetry.Verify(
            t => t.TrackNavigationError(It.IsAny<EvaluationSession>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task JumpToUnknownTarget_KeepsNotFoundTelemetryAndPrompt()
    {
        var speechService = new RecordingSpeechService();
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var handler = new FormVoiceCommandJumpHandler(telemetry.Object, speechService);
        var interpreter = CreateInterpreter();
        var context = CreateJumpContext(interpreter, argument: "gibt es nicht");

        await handler.HandleAsync(context, CancellationToken.None);


        telemetry.Verify(
            t => t.TrackNavigationError(It.IsAny<EvaluationSession>(), NavigationErrorCodes.JumpTargetNotFound, It.IsAny<string>()),
            Times.Once);
        var call = Assert.Single(speechService.Calls);
        Assert.DoesNotContain(InstructionText, call.Text, StringComparison.Ordinal);
        Assert.Equal("refField", (await interpreter.GetFocusedFieldAsync()).FieldId);
    }

    [Fact]
    public async Task JumpToNameFieldVoiceHintSynonym_FocusesEditableNameField_NotTheReadOnlyGuidanceField()
    {
        // Das Eingabefeld heißt per Synonym „Vorname“, das Hinweisfeld „Vorname Erklaerung“.
        // Der genaue Sprung zu „Vorname“ bleibt beim Eingabefeld, ohne Ausgabeaufruf,
        // Aufnahme oder Navigationsfehler. Die Reihenfolge einer gesprochenen Anleitung
        // wird in diesem Test nicht ausgeführt.
        var speechService = new RecordingSpeechService();
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var handler = new FormVoiceCommandJumpHandler(telemetry.Object, speechService);
        var interpreter = CreateInterpreterWithGuidedNamePair();
        var context = CreateJumpContext(interpreter, argument: "Vorname");

        Assert.Equal("nameField", (await interpreter.GetFocusedFieldAsync()).FieldId);

        await handler.HandleAsync(context, CancellationToken.None);

        Assert.Equal("nameField", (await interpreter.GetFocusedFieldAsync()).FieldId);
        Assert.Empty(speechService.Calls);
        telemetry.Verify(
            t => t.TrackNavigationError(It.IsAny<EvaluationSession>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task TryJumpToAsync_PortOutcomes_AllThreeKindsObservableThroughTheRuntimeContract()
    {

        IFormDialogRuntime runtime = CreateInterpreter();


        var notFound = await runtime.TryJumpToAsync("gibt es nicht");
        Assert.Equal(FormJumpToResultKind.NotFound, notFound.Kind);
        Assert.Empty(notFound.Readbacks);

        // Das Hinweisfeld liefert den für das Vorlesen freigegebenen Wert.
        var spoken = await runtime.TryJumpToAsync("Hinweis");
        Assert.Equal(FormJumpToResultKind.SpokenReadOnlyTarget, spoken.Kind);
        var readback = Assert.Single(spoken.Readbacks);
        Assert.Equal("Hinweis", readback.Label);
        Assert.Equal(InstructionText, readback.SpokenValue);
        Assert.Equal(FormSpokenValueProjectionStatus.FullValue, readback.Status);
        Assert.Equal("refField", (await runtime.GetFocusedFieldAsync()).FieldId);

        // Ein Eingabeziel verschiebt den Fokus ohne Vorleseinhalte.
        var focused = await runtime.TryJumpToAsync("Stadt");
        Assert.Equal(FormJumpToResultKind.Focused, focused.Kind);
        Assert.Empty(focused.Readbacks);
        Assert.Equal("cityField", (await runtime.GetFocusedFieldAsync()).FieldId);
    }

    [Fact]
    public async Task JumpToReadOnlyOnlyGroup_SpeaksGroupIntroAndValuedContent_AsSuccess()
    {
        // Für die Hinweisgruppe werden Gruppentitel und vorhandener Inhalt zur Ausgabe
        // übergeben. Das leere Feld wird übersprungen; Fokus und Fehlertelemetrie bleiben unverändert.
        var speechService = new RecordingSpeechService();
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var handler = new FormVoiceCommandJumpHandler(telemetry.Object, speechService);
        var interpreter = CreateInterpreterWithReadOnlyOnlyGroup();
        var context = CreateJumpContext(interpreter, argument: "Hinweise");

        Assert.Equal("refField", (await interpreter.GetFocusedFieldAsync()).FieldId);

        await handler.HandleAsync(context, CancellationToken.None);

        Assert.Equal("refField", (await interpreter.GetFocusedFieldAsync()).FieldId);
        telemetry.Verify(
            t => t.TrackNavigationError(It.IsAny<EvaluationSession>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        Assert.Equal(2, speechService.Calls.Count);
        Assert.Equal("Feldgruppe Hinweise.", speechService.Calls[0].Text);
        Assert.Equal($"Hinweis ist {InstructionText}", speechService.Calls[1].Text);
    }

    [Fact]
    public async Task JumpToReadOnlyFieldWithoutValue_SpeaksLocalizedNoContentPhrase()
    {
        // Beim direkten Sprung zu einem leeren Hinweisfeld enthält der Ausgabetext
        // dessen Bezeichnung und den Hinweis auf den fehlenden Inhalt.
        var speechService = new RecordingSpeechService();
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var handler = new FormVoiceCommandJumpHandler(telemetry.Object, speechService);
        var interpreter = CreateInterpreterWithReadOnlyOnlyGroup();
        var context = CreateJumpContext(interpreter, argument: "Leerhinweis");

        await handler.HandleAsync(context, CancellationToken.None);

        telemetry.Verify(
            t => t.TrackNavigationError(It.IsAny<EvaluationSession>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        var call = Assert.Single(speechService.Calls);
        Assert.Equal("Leerhinweis hat keinen Inhalt.", call.Text);
        Assert.Equal("refField", (await interpreter.GetFocusedFieldAsync()).FieldId);
    }

    [Theory]
    [InlineData("Vertraulich", SensitiveText)]
    [InlineData("Geheim", SecretText)]
    public async Task JumpToSensitiveOrSecretReadOnlyField_SpeaksLabelOnly_NeverValueContent(string label, string rawValue)
    {
        // Der Sprung zum vorbelegten vertraulichen Feld erteilt keine Zustimmung zum Vorlesen.
        // In diesen Fällen darf der Ausgabeaufruf nur den Hinweis auf die zurückgehaltene
        // Information enthalten, auch keine Wertendung oder Längenangabe.
        var speechService = new RecordingSpeechService();
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var handler = new FormVoiceCommandJumpHandler(telemetry.Object, speechService);
        var interpreter = CreateInterpreterWithPrivacyClassedFields();
        var context = CreateJumpContext(interpreter, argument: label);

        await handler.HandleAsync(context, CancellationToken.None);

        var call = Assert.Single(speechService.Calls);
        // Der vollständige Textvergleich schließt auch eine Wertendung oder Längenangabe aus.
        // Die zusätzliche Prüfung macht das Fehlen des Rohwerts ausdrücklich sichtbar.
        Assert.Equal($"Zur Information. Das Feld {label} wird nicht automatisch vorgelesen.", call.Text);
        Assert.DoesNotContain(rawValue, call.Text, StringComparison.Ordinal);
        telemetry.Verify(
            t => t.TrackNavigationError(It.IsAny<EvaluationSession>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task PositionAndProgressTotals_CountOnlyAnswerableFields()
    {
        // Schreibgeschützte Hinweise sind keine bearbeitbaren Schritte und erhöhen deshalb
        // nicht die Gesamtzahl der Eingabefelder im Fortschritt.
        var interpreter = CreateInterpreter();

        var statusSnapshot = await interpreter.GetStatusSnapshotAsync();

        // refField und cityField sind bearbeitbar, instructionField ist nur sichtbar.
        Assert.Equal(2, statusSnapshot.Progress.AllFields.Total);
        Assert.Equal(1, statusSnapshot.CurrentFieldIndex);
    }

    [Fact]
    public async Task DirectFocus_OnReadOnlyField_IsRefused_AndFocusStaysPut()
    {
        // Die direkte Fokussetzung lehnt das Hinweisfeld ab und lässt den bisherigen Fokus bestehen.
        var interpreter = CreateInterpreter();
        Assert.Equal("refField", (await interpreter.GetFocusedFieldAsync()).FieldId);

        Assert.False(await interpreter.TrySetFocusToFieldIdAsync("instructionField"));
        Assert.Equal("refField", (await interpreter.GetFocusedFieldAsync()).FieldId);
    }

    [Fact]
    public async Task OpenRequiredNavigation_NeverFocusesTheRequiredReadOnlyField()
    {
        // Ein leeres schreibgeschütztes Pflichtfeld zählt als offen, kann aber nicht fokussiert werden.
        // Die Navigation überspringt es und meldet das aktuelle Eingabefeld als einziges erreichbares Ziel.
        var interpreter = CreateInterpreterWithRequiredHazard();
        Assert.Equal("refField", (await interpreter.GetFocusedFieldAsync()).FieldId);


        var snapshot = await interpreter.GetStatusSnapshotAsync();
        Assert.Equal(2, snapshot.OpenRequiredFieldCount);

        var result = await interpreter.TryFocusNextOpenRequiredFieldAsync();


        Assert.Equal(FocusNextOpenRequiredFieldResultKind.AlreadyOnSoleTarget, result.Kind);
        Assert.Equal("refField", (await interpreter.GetFocusedFieldAsync()).FieldId);
    }

    [Fact]
    public async Task RequiredReadOnlyFieldWithValue_CountsCompleted_LikeToday()
    {
        // Der vorbelegte Wert erfüllt das schreibgeschützte Pflichtfeld.
        // Damit bleibt nur das leere bearbeitbare Pflichtfeld offen.
        var interpreter = CreateInterpreterWithRequiredValuedReadOnly();

        var snapshot = await interpreter.GetStatusSnapshotAsync();

        Assert.Equal(1, snapshot.OpenRequiredFieldCount);
    }

    [Fact]
    public async Task Summary_OmitsTheReadOnlyFieldValue()
    {
        // Die Zusammenfassung enthält Eingabeantworten; der vorbelegte Hinweiswert gehört nicht dazu.
        var interpreter = CreateInterpreter();

        var items = await interpreter.BuildSummaryItemsAsync();

        Assert.DoesNotContain(items, item => item.FieldId == "instructionField");
    }

    [Fact]
    public async Task JumpToAllValuelessReadOnlyGroup_SpeaksOnlyTheGroupIntro_AsSuccess()
    {
        // Für eine Hinweisgruppe ohne Werte wird nur der Gruppentitel zur Ausgabe übergeben.
        // Es entsteht kein Navigationsfehler, und der Eingabefokus bleibt bestehen.
        var speechService = new RecordingSpeechService();
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var handler = new FormVoiceCommandJumpHandler(telemetry.Object, speechService);
        var interpreter = CreateInterpreterWithAllValuelessReadOnlyGroup();
        var context = CreateJumpContext(interpreter, argument: "Hinweise");

        await handler.HandleAsync(context, CancellationToken.None);

        telemetry.Verify(
            t => t.TrackNavigationError(It.IsAny<EvaluationSession>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        var call = Assert.Single(speechService.Calls);
        Assert.Equal("Feldgruppe Hinweise.", call.Text);
        Assert.Equal("refField", (await interpreter.GetFocusedFieldAsync()).FieldId);
    }

    [Theory]
    [InlineData("sensitiveField")]
    [InlineData("secretField")]
    public async Task OnDemandReadbackProjection_WithholdsSensitiveAndSecretValues(string fieldId)
    {
        // Die Projektion der hier vorbelegten sensiblen und geheimen Werte liefert
        // Withheld und keinen Vorlesewert. Eine Zustimmung zum Vorlesen ist nicht gesetzt.
        var interpreter = CreateInterpreterWithPrivacyClassedFields();
        var projector = new FormReadOnlyFieldReadbackProjector(interpreter, interpreter, interpreter, interpreter, "de");

        var readback = await projector.ProjectFieldReadbackAsync(fieldId);

        Assert.Equal(FormSpokenValueProjectionStatus.Withheld, readback.Status);
        Assert.Null(readback.SpokenValue);
    }

    private static FormInterpreterRuntimeOwner CreateInterpreter()
        => new(
            CreateReadOnlyInstructionContract(),
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));

    private static FormInterpreterRuntimeOwner CreateInterpreterWithReadOnlyOnlyGroup()
        => new(
            CreateReadOnlyOnlyGroupContract(),
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));

    private static FormInterpreterRuntimeOwner CreateInterpreterWithPrivacyClassedFields()
        => new(
            CreatePrivacyClassedReadOnlyContract(),
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));

    private static FormInterpreterRuntimeOwner CreateInterpreterWithRequiredHazard()
        => new(
            CreateRequiredReadOnlyContract(seedInstructionValue: false),
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));

    private static FormInterpreterRuntimeOwner CreateInterpreterWithRequiredValuedReadOnly()
        => new(
            CreateRequiredReadOnlyContract(seedInstructionValue: true),
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));

    private static FormInterpreterRuntimeOwner CreateInterpreterWithGuidedNamePair()
        => new(
            CreateGuidedNamePairContract(),
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));

    private static FormInterpreterRuntimeOwner CreateInterpreterWithAllValuelessReadOnlyGroup()
        => new(
            CreateAllValuelessReadOnlyGroupContract(),
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));

    private static FormVoiceCommandGlobalCommandContext CreateJumpContext(
        FormInterpreterRuntimeOwner interpreter,
        string argument)
    {
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
                CommandId: GlobalCommandIds.JumpTo,
                IsoCode: "de",
                Argument: argument),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings(),
                Application: application,
                CompletedFormIds: ImmutableHashSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase),
                Form: form,
                Contract: interpreter.Contract),
            Runtime: new FormVoiceCommandGlobalCommandRuntime(
                Interpreter: interpreter,
                Session: new EvaluationSession("readonly-m3-session", EvaluationChannel.Voice, "app", "form"),
                StateTracker: new TestFormVoiceCommandDialogStateTracker()));
    }

    // Eigenständiger Beispielvertrag mit vorbelegtem Hinweisfeld vor dem Eingabefeld
    // und getrennten Synonymen für beide. So hängt dieser Strukturtest nicht
    // von späteren Änderungen eines Anwendungskatalogs ab.
    private static FormContract CreateGuidedNamePairContract()
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
            EntryFieldGroupId = FieldGroupId.From("name"),
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("guidanceName"),
                    Name = "guidanceName",
                    ValueType = stringValueType,
                    InitialValue = JsonDocument.Parse("\"Marta\"").RootElement
                },
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
                    FieldId = FieldId.From("guidanceNameField"),
                    VariableId = VariableId.From("guidanceName"),
                    Label = new LocalizedText { ["de"] = "Vorname. Der vorgegebene Wert lautet: Marta." },
                    Editability = FieldEditability.ReadOnly
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("nameField"),
                    VariableId = VariableId.From("name"),
                    Label = new LocalizedText { ["de"] = "Vorname" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("name"),
                    Title = new LocalizedText { ["de"] = "Schritt 1: Vorname" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("guidanceNameField") },
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("nameField") }
                    ]
                }
            ],
            Projections = new ContractProjections
            {
                Voice = new VoiceProjection
                {
                    Hints = new VoiceHintsDefinition
                    {
                        FieldNameSynonyms = new Dictionary<string, SynonymSet>(StringComparer.Ordinal)
                        {
                            ["guidanceNameField"] = new()
                            {
                                PrimaryName = new LocalizedText { ["de"] = "Vorname Erklaerung" },
                                Synonyms = [new LocalizedText { ["de"] = "Vorname Erklaerung" }]
                            },
                            ["nameField"] = new()
                            {
                                PrimaryName = new LocalizedText { ["de"] = "Vorname" },
                                Synonyms = [new LocalizedText { ["de"] = "Vorname" }]
                            }
                        }
                    }
                }
            }
        };
    }

    private static FormContract CreateReadOnlyInstructionContract()
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
                    VariableId = VariableId.From("ref"),
                    Name = "ref",
                    ValueType = stringValueType
                },
                new VariableDefinition
                {
                    VariableId = VariableId.From("instruction"),
                    Name = "instruction",
                    ValueType = stringValueType,
                    InitialValue = JsonDocument.Parse($"\"{InstructionText}\"").RootElement
                },
                new VariableDefinition
                {
                    VariableId = VariableId.From("city"),
                    Name = "city",
                    ValueType = stringValueType
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("refField"),
                    VariableId = VariableId.From("ref"),
                    Label = new LocalizedText { ["de"] = "Referenznummer" },
                    Editability = FieldEditability.UserEditable
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("instructionField"),
                    VariableId = VariableId.From("instruction"),
                    Label = new LocalizedText { ["de"] = "Hinweis" },
                    Editability = FieldEditability.ReadOnly
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("cityField"),
                    VariableId = VariableId.From("city"),
                    Label = new LocalizedText { ["de"] = "Stadt" },
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
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("refField") },
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("instructionField") },
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("cityField") }
                    ]
                }
            ]
        };
    }

    private static FormContract CreateReadOnlyOnlyGroupContract()
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
                    VariableId = VariableId.From("ref"),
                    Name = "ref",
                    ValueType = stringValueType
                },
                new VariableDefinition
                {
                    VariableId = VariableId.From("instruction"),
                    Name = "instruction",
                    ValueType = stringValueType,
                    InitialValue = JsonDocument.Parse($"\"{InstructionText}\"").RootElement
                },
                new VariableDefinition
                {
                    VariableId = VariableId.From("emptyInstruction"),
                    Name = "emptyInstruction",
                    ValueType = stringValueType
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("refField"),
                    VariableId = VariableId.From("ref"),
                    Label = new LocalizedText { ["de"] = "Referenznummer" },
                    Editability = FieldEditability.UserEditable
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("instructionField"),
                    VariableId = VariableId.From("instruction"),
                    Label = new LocalizedText { ["de"] = "Hinweis" },
                    Editability = FieldEditability.ReadOnly
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("emptyInstructionField"),
                    VariableId = VariableId.From("emptyInstruction"),
                    Label = new LocalizedText { ["de"] = "Leerhinweis" },
                    Editability = FieldEditability.ReadOnly
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
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("refField") },
                        // Die Hinweisgruppe muss vom Einstieg aus über ein verschachteltes
                        // Gruppenmitglied erreichbar sein.
                        new FieldGroupMember { Kind = "fieldGroup", FieldGroupId = FieldGroupId.From("info") }
                    ]
                },
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("info"),
                    Title = new LocalizedText { ["de"] = "Hinweise" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("instructionField") },
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("emptyInstructionField") }
                    ]
                }
            ]
        };
    }

    private static FormContract CreateRequiredReadOnlyContract(bool seedInstructionValue)
    {
        var contract = CreateReadOnlyInstructionContract();
        if (!seedInstructionValue)
        {
            // Ohne Anfangswert bleibt das schreibgeschützte Pflichtfeld leer
            // und kann durch Benutzereingabe nicht vervollständigt werden.
            contract.Variables.Single(variable => variable.VariableId.Value == "instruction").InitialValue = null;
        }

        contract.Constraints =
        [
            new ConstraintDefinition
            {
                ConstraintId = "refRequired",
                Target = new ConstraintTargetRef { Kind = "field", FieldId = FieldId.From("refField") },
                Severity = ConstraintSeverity.Error,
                Code = "form.required",
                Message = new LocalizedText { ["de"] = "Dieses Feld ist erforderlich." },
                Kind = ConstraintKind.Required
            },
            new ConstraintDefinition
            {
                ConstraintId = "instructionRequired",
                Target = new ConstraintTargetRef { Kind = "field", FieldId = FieldId.From("instructionField") },
                Severity = ConstraintSeverity.Error,
                Code = "form.required",
                Message = new LocalizedText { ["de"] = "Dieses Feld ist erforderlich." },
                Kind = ConstraintKind.Required
            }
        ];

        return contract;
    }

    private static FormContract CreateAllValuelessReadOnlyGroupContract()
    {
        var contract = CreateReadOnlyOnlyGroupContract();

        // Nach Entfernen des vorbelegten Felds enthält die Hinweisgruppe nur noch ein leeres Feld.
        contract.Variables = contract.Variables.Where(variable => variable.VariableId.Value != "instruction").ToList();
        contract.Fields = contract.Fields.Where(field => field.FieldId.Value != "instructionField").ToList();
        var infoGroup = contract.FieldGroups.Single(fieldGroup => fieldGroup.FieldGroupId.Value == "info");
        infoGroup.Members = infoGroup.Members.Where(member => member.FieldId?.Value != "instructionField").ToList();

        return contract;
    }

    private static FormContract CreatePrivacyClassedReadOnlyContract()
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
                    VariableId = VariableId.From("ref"),
                    Name = "ref",
                    ValueType = stringValueType
                },
                new VariableDefinition
                {
                    VariableId = VariableId.From("sensitiveNote"),
                    Name = "sensitiveNote",
                    ValueType = stringValueType,
                    PrivacyClass = PrivacyClass.Sensitive,
                    InitialValue = JsonDocument.Parse($"\"{SensitiveText}\"").RootElement
                },
                new VariableDefinition
                {
                    VariableId = VariableId.From("secretNote"),
                    Name = "secretNote",
                    ValueType = stringValueType,
                    PrivacyClass = PrivacyClass.Secret,
                    InitialValue = JsonDocument.Parse($"\"{SecretText}\"").RootElement
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("refField"),
                    VariableId = VariableId.From("ref"),
                    Label = new LocalizedText { ["de"] = "Referenznummer" },
                    Editability = FieldEditability.UserEditable
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("sensitiveField"),
                    VariableId = VariableId.From("sensitiveNote"),
                    Label = new LocalizedText { ["de"] = "Vertraulich" },
                    Editability = FieldEditability.ReadOnly
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("secretField"),
                    VariableId = VariableId.From("secretNote"),
                    Label = new LocalizedText { ["de"] = "Geheim" },
                    Editability = FieldEditability.ReadOnly
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
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("refField") },
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("sensitiveField") },
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("secretField") }
                    ]
                }
            ]
        };
    }
}
