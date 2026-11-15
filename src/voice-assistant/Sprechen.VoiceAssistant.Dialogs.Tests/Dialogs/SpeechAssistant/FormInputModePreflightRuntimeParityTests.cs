using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Mda.Contracts.V1.Preflight;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Vergleicht Vorabprüfung und Laufzeitverfügbarkeit an denselben Beispielverträgen.
/// Die Fälle prüfen verbotene Mikrofonmodi, zulässige Eingaben und einen für den Feldtyp
/// ungültigen bevorzugten Modus.
/// </summary>
public sealed class FormInputModePreflightRuntimeParityTests
{
    [Theory]
    [InlineData("voice")]
    [InlineData("spelling")]
    [InlineData("digits")]
    [InlineData("literal")]
    public void SecretField_PreflightRejectsPreferredMicrophoneMode_RuntimeBlocksTheSameMode(string microphoneMode)
    {
        // Der Feldtyp erlaubt den jeweiligen Mikrofonmodus grundsätzlich; für Zifferneingabe
        // ist ein passender Formathinweis gesetzt. So entscheidet hier die Vertraulichkeitsklasse.
        var contract = CreateContract(
            PrivacyClass.Secret,
            preferredInputModes: ["keyboard", microphoneMode],
            valueTypeJson: DigitFriendlyValueTypeJson(microphoneMode));

        var preflight = FormContractPreflight.Validate(contract);
        var availability = EvaluateRuntime(contract, microphoneMode);

        Assert.False(preflight.Ok);
        Assert.Equal(FormFieldInputModeAvailabilityStatus.Unavailable, availability.Status);
        Assert.Equal(FormFieldInputModeUnavailabilityReason.BlockedByPrivacyPolicy, availability.UnavailabilityReason);
    }

    [Fact]
    public void SecretField_PreflightAcceptsKeyboardOnly_RuntimeKeepsKeyboardAvailable()
    {
        var contract = CreateContract(PrivacyClass.Secret, preferredInputModes: ["keyboard"]);

        var preflight = FormContractPreflight.Validate(contract);
        var availability = EvaluateRuntime(contract, "keyboard");

        Assert.True(preflight.Ok);
        Assert.Equal(FormFieldInputModeAvailabilityStatus.Available, availability.Status);
    }

    [Theory]
    [InlineData("spelling")]
    [InlineData("digits")]
    public void SensitiveField_PreflightAndRuntimeAdmitComposedModesWhenVoiceIsPermitted(string composedMode)
    {
        var contract = CreateContract(
            PrivacyClass.Sensitive,
            preferredInputModes: [composedMode, "keyboard"],
            valueTypeJson: DigitFriendlyValueTypeJson(composedMode));

        var preflight = FormContractPreflight.Validate(contract);
        var availability = EvaluateRuntime(contract, composedMode);

        Assert.True(preflight.Ok);
        Assert.Equal(FormFieldInputModeAvailabilityStatus.Available, availability.Status);
    }

    [Fact]
    public void SensitiveField_PreflightAcceptsVoice_RuntimeKeepsVoiceAvailable()
    {
        var contract = CreateContract(PrivacyClass.Sensitive, preferredInputModes: ["voice", "keyboard"]);

        var preflight = FormContractPreflight.Validate(contract);
        var availability = EvaluateRuntime(contract, "voice");

        Assert.True(preflight.Ok);
        Assert.Equal(FormFieldInputModeAvailabilityStatus.Available, availability.Status);
    }

    [Fact]
    public void PersonalField_PreflightAcceptsSpelling_RuntimeKeepsSpellingAvailable()
    {
        var contract = CreateContract(PrivacyClass.Personal, preferredInputModes: ["spelling", "voice"]);

        var preflight = FormContractPreflight.Validate(contract);
        var availability = EvaluateRuntime(contract, "spelling");

        Assert.True(preflight.Ok);
        Assert.Equal(FormFieldInputModeAvailabilityStatus.Available, availability.Status);
    }

    [Fact]
    public void AuthoredIncompatiblePreferredMode_PreflightRejectsAndRuntimeNeverResolvesIt()
    {
        // Zifferneingabe ist für dieses Textfeld ohne passenden Formathinweis nicht verfügbar.
        var contract = CreateContract(
            PrivacyClass.Personal,
            preferredInputModes: ["digits", "voice"]);

        var preflight = FormContractPreflight.Validate(contract);
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var entryMode = interpreter.GetFocusedFieldAsync().GetAwaiter().GetResult().InputMode;


        Assert.False(preflight.Ok);
        Assert.Contains(preflight.Errors, issue => issue.Code == PreflightCodes.ErrorFeatureNotSupported);
        Assert.NotEqual(InputMode.Digits, entryMode);
        Assert.Equal(InputMode.Voice, entryMode);
    }

    [Fact]
    public void IbanFixtureShape_SensitiveSpellingFirst_IsPreflightCleanAndAvailable()
    {
        var contract = CreateContract(
            PrivacyClass.Sensitive,
            sensitiveDataPolicy: new SensitiveDataPolicy
            {
                ReadbackPolicy = SensitiveDataPolicyDefaults.ReadbackNever
            },
            preferredInputModes: ["spelling", "keyboard", "voice"],
            valueTypeJson: """{"kind":"string","formatHint":"iban"}""");

        var preflight = FormContractPreflight.Validate(contract);
        var availability = EvaluateRuntime(contract, "spelling");

        Assert.True(preflight.Ok);
        Assert.Equal(FormFieldInputModeAvailabilityStatus.Available, availability.Status);
    }

    private static string DigitFriendlyValueTypeJson(string modeWire)
        => modeWire == "digits" ? """{"kind":"string","formatHint":"phone"}""" : """{"kind":"string"}""";

    private static FormFieldInputModeAvailability EvaluateRuntime(FormContract contract, string modeWire)
    {
        Assert.True(InputModeExtensions.TryFromWireValue(modeWire, out var mode));
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        return ((IFormDialogRuntime)interpreter)
            .GetFocusedFieldInputModeAvailabilityAsync(modeWire)
            .GetAwaiter()
            .GetResult();
    }

    private static FormContract CreateContract(
        PrivacyClass privacyClass,
        IReadOnlyList<string> preferredInputModes,
        SensitiveDataPolicy? sensitiveDataPolicy = null,
        string valueTypeJson = """{"kind":"string"}""")
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "test-app",
            FormId = FormId.From("test-form"),
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
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
                    Editability = FieldEditability.UserEditable,
                    PreferredInputModes = preferredInputModes.ToList()
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("field") }]
                }
            ]
        };
    }
}
