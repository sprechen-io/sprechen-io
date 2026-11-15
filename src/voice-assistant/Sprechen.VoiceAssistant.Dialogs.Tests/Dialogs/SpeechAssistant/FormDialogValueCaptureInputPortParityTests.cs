using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Vergleicht für ein boolesches Feld die Verfügbarkeitsabfrage und den Moduswechsel
/// über die Laufzeitschnittstelle. Weitere Fälle prüfen Buchstabieren bei einem sensiblen
/// Textfeld und die Ablehnung eines ungültigen booleschen Werts.
/// </summary>
public sealed class FormDialogValueCaptureInputPortParityTests
{
    public static TheoryData<string> MicrophoneAndKeyboardModes => new()
    {
        InputModes.Voice,
        InputModes.Keyboard,
        InputModes.Spelling,
        InputModes.Digits,
        InputModes.Literal
    };

    [Theory]
    [MemberData(nameof(MicrophoneAndKeyboardModes))]
    public async Task AdmissionAndAvailability_AgreeOnEveryMode(string inputMode)
    {
        var interpreter = CreateInterpreter("""{"kind":"boolean"}""");
        var port = (IFormDialogRuntime)interpreter;

        var availability = await port.GetFocusedFieldInputModeAvailabilityAsync(inputMode);
        var admitted = await port.TrySetFocusedFieldInputModeAsync(inputMode);


        Assert.Equal(availability.Status == FormFieldInputModeAvailabilityStatus.Available, admitted);
    }

    [Fact]
    public async Task SensitiveSpelling_ThroughThePort_IsAvailableWithoutAGrant()
    {
        var interpreter = CreateInterpreter(
            """{"kind":"string"}""",
            privacyClass: PrivacyClass.Sensitive,
            sensitiveDataPolicy: new SensitiveDataPolicy
            {
                ReadbackPolicy = SensitiveDataPolicyDefaults.ReadbackNever
            });
        var port = (IFormDialogRuntime)interpreter;

        var availability = await port.GetFocusedFieldInputModeAvailabilityAsync(InputModes.Spelling);
        Assert.Equal(FormFieldInputModeAvailabilityStatus.Available, availability.Status);
        Assert.True(await port.TrySetFocusedFieldInputModeAsync(InputModes.Spelling));
        Assert.Equal(InputMode.Spelling, (await port.GetFocusedFieldAsync()).InputMode);
    }

    [Fact]
    public async Task ValueApplication_ThroughThePort_AppliesTheSameTypedGate()
    {
        var interpreter = CreateInterpreter("""{"kind":"boolean"}""");
        var port = (IFormDialogRuntime)interpreter;

        // Auch der Einstieg für zusammengesetzte Werte darf „vielleicht“ nicht als booleschen Wert übernehmen.
        var result = await port.TryApplyComposedValueAsync("vielleicht");

        Assert.False(result.ValueCaptured);
        Assert.Empty(await interpreter.GetValuesSnapshotAsync());
    }

    private static FormInterpreterRuntimeOwner CreateInterpreter(
        string valueTypeJson,
        PrivacyClass? privacyClass = null,
        SensitiveDataPolicy? sensitiveDataPolicy = null)
    {
        var contract = new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("form"),
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            EntryFieldGroupId = FieldGroupId.From("main"),
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("variable"),
                    Name = "variable",
                    PrivacyClass = privacyClass,
                    SensitiveDataPolicy = sensitiveDataPolicy,
                    ValueType = JsonDocument.Parse(valueTypeJson).RootElement
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("field"),
                    VariableId = VariableId.From("variable"),
                    Label = new LocalizedText { ["de"] = "Feld" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members = [new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("field") }]
                }
            ]
        };
        return new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
    }
}
