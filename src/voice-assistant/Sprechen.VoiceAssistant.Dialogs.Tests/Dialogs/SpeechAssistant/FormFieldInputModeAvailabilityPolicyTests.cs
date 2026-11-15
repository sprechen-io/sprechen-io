using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormPrivacy;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormFieldInputModeAvailabilityPolicyTests
{
    public static TheoryData<string, bool, InputMode, string, string?> BaseMatrix
    {
        get
        {
            var data = new TheoryData<string, bool, InputMode, string, string?>();

            // Sprache und Tastatur sind für alle hier geprüften Feldtypen verfügbar.
            foreach (var (valueTypeKind, codeStyle) in EffectiveTypeShapes())
            {
                data.Add(valueTypeKind, codeStyle, InputMode.Voice, "Available", null);
                data.Add(valueTypeKind, codeStyle, InputMode.Keyboard, "Available", null);
            }

            // Buchstabieren und wörtliche Eingabe sind nur für Text- und Codefelder verfügbar.
            foreach (var (valueTypeKind, codeStyle) in EffectiveTypeShapes())
            {
                var composable = valueTypeKind == "string";
                data.Add(
                    valueTypeKind,
                    codeStyle,
                    InputMode.Spelling,
                    composable ? "Available" : "Unavailable",
                    composable ? null : "UnsupportedForFieldType");
                data.Add(
                    valueTypeKind,
                    codeStyle,
                    InputMode.Literal,
                    composable ? "Available" : "Unavailable",
                    composable ? null : "UnsupportedForFieldType");
            }

            // Zifferneingabe ist für Code und Ganzzahlen verfügbar. Text benötigt einen geeigneten
            // Formathinweis; die folgenden Tests prüfen diese Hinweise separat.
            foreach (var (valueTypeKind, codeStyle) in EffectiveTypeShapes())
            {
                var digitFriendly = valueTypeKind == "integer" || codeStyle;
                data.Add(
                    valueTypeKind,
                    codeStyle,
                    InputMode.Digits,
                    digitFriendly ? "Available" : "Unavailable",
                    digitFriendly ? null : "UnsupportedForFieldType");
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(BaseMatrix))]
    public void Evaluate_BaseTypeModeMatrix_ReturnsTheFrozenDecision(
        string valueTypeKind,
        bool codeStyle,
        InputMode mode,
        string expectedStatus,
        string? expectedReason)
    {
        var variable = CreateVariable(ValueTypeJson(valueTypeKind));
        var policy = CreateAvailability(variable);

        var availability = policy.Evaluate(
            CreateField(codeStyle: codeStyle),
            variable,
            mode);

        Assert.Equal(Enum.Parse<FormFieldInputModeAvailabilityStatus>(expectedStatus), availability.Status);
        Assert.Equal(
            expectedReason is null ? null : Enum.Parse<FormFieldInputModeUnavailabilityReason>(expectedReason),
            availability.UnavailabilityReason);
    }

    [Theory]
    [InlineData("phone")]
    [InlineData("postalCode")]
    [InlineData("pin")]
    [InlineData("digits")]
    public void Evaluate_WhenTextFieldCarriesDigitFriendlyFormatHint_AllowsDigits(string formatHint)
    {
        var variable = CreateVariable($$"""{"kind":"string","formatHint":"{{formatHint}}"}""");
        var policy = CreateAvailability(variable);

        var availability = policy.Evaluate(CreateField(), variable, InputMode.Digits);

        Assert.Equal(FormFieldInputModeAvailabilityStatus.Available, availability.Status);
        Assert.Null(availability.UnavailabilityReason);
    }

    [Theory]
    [InlineData("email")]
    [InlineData("iban")]
    [InlineData("date")]
    public void Evaluate_WhenTextFieldCarriesNonDigitFormatHint_RejectsDigits(string formatHint)
    {
        var variable = CreateVariable($$"""{"kind":"string","formatHint":"{{formatHint}}"}""");
        var policy = CreateAvailability(variable);

        var availability = policy.Evaluate(CreateField(), variable, InputMode.Digits);

        Assert.Equal(FormFieldInputModeAvailabilityStatus.Unavailable, availability.Status);
        Assert.Equal(FormFieldInputModeUnavailabilityReason.UnsupportedForFieldType, availability.UnavailabilityReason);
    }

    [Theory]
    [InlineData(InputMode.Voice)]
    [InlineData(InputMode.Keyboard)]
    [InlineData(InputMode.Spelling)]
    [InlineData(InputMode.Digits)]
    [InlineData(InputMode.Literal)]
    public void Evaluate_WhenFieldDeclaredReadOnly_RejectsEveryModeAsNotEditable(InputMode mode)
    {
        // Das Codefeld erlaubt grundsätzlich alle geprüften Modi. Damit entscheidet hier
        // ausschließlich der Schreibschutz über die Verfügbarkeit.
        var variable = CreateVariable(ValueTypeJson("string"));
        var policy = CreateAvailability(variable);

        var availability = policy.Evaluate(
            CreateField(codeStyle: true, editability: FieldEditability.ReadOnly),
            variable,
            mode);

        Assert.Equal(FormFieldInputModeAvailabilityStatus.Unavailable, availability.Status);
        Assert.Equal(FormFieldInputModeUnavailabilityReason.FieldNotEditable, availability.UnavailabilityReason);
    }

    [Fact]
    public void Evaluate_WhenFieldRuntimeReadOnly_RejectsAsNotEditable()
    {
        var store = FormMutableStateStoreTestFactory.Create();
        store.SetFieldReadOnly(FieldId.From("field"), readOnly: true);
        var variable = CreateVariable(ValueTypeJson("string"));
        var policy = CreateAvailability(variable, store);

        var availability = policy.Evaluate(CreateField(), variable, InputMode.Voice);

        Assert.Equal(FormFieldInputModeAvailabilityStatus.Unavailable, availability.Status);
        Assert.Equal(FormFieldInputModeUnavailabilityReason.FieldNotEditable, availability.UnavailabilityReason);
    }

    [Theory]
    [InlineData(InputMode.Voice)]
    [InlineData(InputMode.Spelling)]
    [InlineData(InputMode.Digits)]
    [InlineData(InputMode.Literal)]
    public void Evaluate_WhenVoiceInputForbidden_RejectsEveryMicrophoneMode(InputMode mode)
    {
        var variable = CreateVariable(
            ValueTypeJson("string"),
            sensitiveDataPolicy: new SensitiveDataPolicy
            {
                VoiceInputPolicy = SensitiveDataPolicyDefaults.VoiceInputForbidden,
                SilentInputFallback = SensitiveDataPolicyDefaults.SilentInputFallbackKeyboard
            });
        var policy = CreateAvailability(variable);

        // Das Codefeld erlaubt grundsätzlich alle Mikrofonmodi. Damit entscheidet hier
        // ausschließlich das Verbot der Spracheingabe über die Verfügbarkeit.
        var availability = policy.Evaluate(CreateField(codeStyle: true), variable, mode);

        Assert.Equal(FormFieldInputModeAvailabilityStatus.Unavailable, availability.Status);
        Assert.Equal(FormFieldInputModeUnavailabilityReason.VoiceInputForbidden, availability.UnavailabilityReason);
    }

    [Fact]
    public void Evaluate_WhenVoiceInputForbidden_KeepsKeyboardAvailable()
    {
        var variable = CreateVariable(
            ValueTypeJson("string"),
            sensitiveDataPolicy: new SensitiveDataPolicy
            {
                VoiceInputPolicy = SensitiveDataPolicyDefaults.VoiceInputForbidden,
                SilentInputFallback = SensitiveDataPolicyDefaults.SilentInputFallbackKeyboard
            });
        var policy = CreateAvailability(variable);

        var availability = policy.Evaluate(CreateField(), variable, InputMode.Keyboard);

        Assert.Equal(FormFieldInputModeAvailabilityStatus.Available, availability.Status);
        Assert.Null(availability.UnavailabilityReason);
    }

    [Theory]
    [InlineData(InputMode.Voice)]
    [InlineData(InputMode.Spelling)]
    [InlineData(InputMode.Digits)]
    [InlineData(InputMode.Literal)]
    public void Evaluate_WhenVariableSecret_RejectsEveryMicrophoneMode(InputMode mode)
    {
        var variable = CreateVariable(ValueTypeJson("string"), privacyClass: PrivacyClass.Secret);
        var policy = CreateAvailability(variable);

        // Das Codefeld erlaubt grundsätzlich alle Mikrofonmodi. Damit entscheidet hier
        // ausschließlich die Vertraulichkeitsklasse über die Verfügbarkeit.
        var availability = policy.Evaluate(CreateField(codeStyle: true), variable, mode);

        Assert.Equal(FormFieldInputModeAvailabilityStatus.Unavailable, availability.Status);
        Assert.Equal(FormFieldInputModeUnavailabilityReason.BlockedByPrivacyPolicy, availability.UnavailabilityReason);
    }

    [Fact]
    public void Evaluate_WhenVariableSecret_KeepsKeyboardAvailable()
    {
        var variable = CreateVariable(ValueTypeJson("string"), privacyClass: PrivacyClass.Secret);
        var policy = CreateAvailability(variable);

        var availability = policy.Evaluate(CreateField(), variable, InputMode.Keyboard);

        Assert.Equal(FormFieldInputModeAvailabilityStatus.Available, availability.Status);
        Assert.Null(availability.UnavailabilityReason);
    }

    [Theory]
    [InlineData(InputMode.Spelling)]
    [InlineData(InputMode.Digits)]
    [InlineData(InputMode.Voice)]
    public void Evaluate_WhenSensitiveAndVoicePermitted_TreatsComposedModesLikeOrdinaryVoice(InputMode mode)
    {
        var variable = CreateVariable(
            ValueTypeJson("string"),
            privacyClass: PrivacyClass.Sensitive,
            sensitiveDataPolicy: new SensitiveDataPolicy
            {
                ReadbackPolicy = SensitiveDataPolicyDefaults.ReadbackNever
            });
        var policy = CreateAvailability(variable);

        var availability = policy.Evaluate(CreateField(codeStyle: true), variable, mode);

        Assert.Equal(FormFieldInputModeAvailabilityStatus.Available, availability.Status);
        Assert.Null(availability.UnavailabilityReason);
    }

    [Fact]
    public void Evaluate_WhenSensitiveVoiceMode_KeepsBaseDecision()
    {
        var variable = CreateVariable(ValueTypeJson("string"), privacyClass: PrivacyClass.Sensitive);
        var policy = CreateAvailability(variable);

        var availability = policy.Evaluate(CreateField(), variable, InputMode.Voice);

        Assert.Equal(FormFieldInputModeAvailabilityStatus.Available, availability.Status);
        Assert.Null(availability.UnavailabilityReason);
    }

    [Theory]
    [InlineData("public")]
    [InlineData("personal")]
    public void Evaluate_WhenPublicOrPersonal_KeepsBaseDecision(string privacyClassToken)
    {
        Assert.True(PrivacyClassExtensions.TryFromWireToken(privacyClassToken, out var privacyClass));
        var variable = CreateVariable(ValueTypeJson("string"), privacyClass: privacyClass);
        var policy = CreateAvailability(variable);

        var availability = policy.Evaluate(CreateField(), variable, InputMode.Spelling);

        Assert.Equal(FormFieldInputModeAvailabilityStatus.Available, availability.Status);
        Assert.Null(availability.UnavailabilityReason);
    }

    [Fact]
    public void Evaluate_WhenPreferredInputModesListAnUnsupportedMode_DoesNotGrantPermission()
    {
        var variable = CreateVariable(ValueTypeJson("boolean"));
        var policy = CreateAvailability(variable);
        var field = CreateField(preferredInputModes: [InputModes.Spelling]);

        var availability = policy.Evaluate(field, variable, InputMode.Spelling);

        Assert.Equal(FormFieldInputModeAvailabilityStatus.Unavailable, availability.Status);
        Assert.Equal(FormFieldInputModeUnavailabilityReason.UnsupportedForFieldType, availability.UnavailabilityReason);
    }

    [Fact]
    public void Evaluate_WhenFieldIsNull_Throws()
    {
        var variable = CreateVariable(ValueTypeJson("string"));
        var policy = CreateAvailability(variable);

        Assert.Throws<ArgumentNullException>(
            () => { _ = policy.Evaluate(field: null!, variable, InputMode.Voice); });
    }

    [Fact]
    public void Evaluate_WhenVariableIsNull_Throws()
    {
        var policy = CreateAvailability(CreateVariable(ValueTypeJson("string")));

        Assert.Throws<ArgumentNullException>(
            () => { _ = policy.Evaluate(CreateField(), variable: null!, InputMode.Voice); });
    }

    [Fact]
    public void ResolveInputMode_WhenFirstPreferredModeIsTypeIncompatible_SkipsToTheNextAvailable()
    {
        // Ohne geeigneten Formathinweis ist Zifferneingabe für dieses Textfeld nicht verfügbar.
        // Die Auflösung soll deshalb den nächsten zulässigen Modus wählen.
        var variable = CreateVariable(ValueTypeJson("string"));
        var policy = CreateAvailability(variable);
        var field = CreateField(preferredInputModes: [InputModes.Digits, InputModes.Voice]);

        var resolved = policy.ResolveInputMode(field, variable, field.FieldId);

        Assert.Equal(InputMode.Voice, resolved);
    }

    [Fact]
    public void ResolveInputMode_WhenPreferredModeIsConsentGated_KeepsItForTheConsentContinuation()
    {
        // Für das sensible Codefeld ist Buchstabieren bevorzugt und eine Zustimmung zum Vorlesen
        // vorgesehen. Die Auflösung soll den bevorzugten Modus beibehalten.
        var field = CreateField(codeStyle: true, preferredInputModes: [InputModes.Spelling, InputModes.Keyboard, InputModes.Voice]);
        var variable = CreateVariable(
            ValueTypeJson("string"),
            privacyClass: PrivacyClass.Sensitive,
            sensitiveDataPolicy: new SensitiveDataPolicy
            {
                ReadbackPolicy = SensitiveDataPolicyDefaults.ReadbackOptInRequired
            });
        var policy = CreateAvailability(variable);

        var resolved = policy.ResolveInputMode(field, variable, field.FieldId);

        Assert.Equal(InputMode.Spelling, resolved);
    }

    [Fact]
    public void ResolveInputMode_WhenStoredOverrideExists_ResolvesItAsIs()
    {
        // Die Auflösung gibt den gespeicherten Modus unverändert zurück.
        // Die Verfügbarkeit wird erst vor der Aufnahme geprüft.
        var store = FormMutableStateStoreTestFactory.Create();
        store.SetFieldInputModeOverride(FieldId.From("field"), InputModes.Keyboard);
        var variable = CreateVariable(ValueTypeJson("string"));
        var policy = CreateAvailability(variable, store);

        var resolved = policy.ResolveInputMode(
            CreateField(preferredInputModes: [InputModes.Voice]),
            variable,
            "field");

        Assert.Equal(InputMode.Keyboard, resolved);
    }

    [Fact]
    public void ResolveInputMode_WhenVoiceIsForbidden_StillResolvesVoiceSoTheGuardCanDeny()
    {
        // Die Auflösung behält den bevorzugten Sprachmodus bei.
        // Das Verbot muss vor der Aufnahme geprüft werden.
        var field = CreateField(preferredInputModes: [InputModes.Voice]);
        var variable = CreateVariable(
            ValueTypeJson("string"),
            sensitiveDataPolicy: new SensitiveDataPolicy
            {
                VoiceInputPolicy = SensitiveDataPolicyDefaults.VoiceInputForbidden,
                SilentInputFallback = SensitiveDataPolicyDefaults.SilentInputFallbackKeyboard
            });
        var policy = CreateAvailability(variable);

        var resolved = policy.ResolveInputMode(field, variable, field.FieldId);

        Assert.Equal(InputMode.Voice, resolved);
    }

    private static FormFieldInputModeAvailabilityPolicy CreateAvailability(
        VariableDefinition variable,
        FormMutableStateStore? store = null)
        => new(store ?? FormMutableStateStoreTestFactory.Create(), FormPrivacyTestFixture.ResolvePolicies([variable]));

    private static IEnumerable<(string ValueTypeKind, bool CodeStyle)> EffectiveTypeShapes()
    {
        yield return ("string", false);
        yield return ("string", true);
        yield return ("integer", false);
        yield return ("boolean", false);
        yield return ("date", false);
        yield return ("time", false);
        yield return ("enum", false);
        yield return ("set", false);
    }

    private static string ValueTypeJson(string valueTypeKind)
        => $$"""{"kind":"{{valueTypeKind}}"}""";

    private static FieldDefinition CreateField(
        string fieldId = "field",
        bool codeStyle = false,
        FieldEditability editability = FieldEditability.UserEditable,
        IReadOnlyList<string>? preferredInputModes = null)
        => new()
        {
            FieldId = FieldId.From(fieldId),
            VariableId = VariableId.From("variable"),
            Label = new LocalizedText { ["de"] = fieldId },
            Editability = editability,
            CodeStyle = codeStyle,
            PreferredInputModes = preferredInputModes?.ToList()
        };

    private static VariableDefinition CreateVariable(
        string valueTypeJson,
        PrivacyClass? privacyClass = null,
        SensitiveDataPolicy? sensitiveDataPolicy = null)
        => new()
        {
            VariableId = VariableId.From("variable"),
            Name = "variable",
            PrivacyClass = privacyClass,
            SensitiveDataPolicy = sensitiveDataPolicy,
            ValueType = ParseJsonElement(valueTypeJson)
        };

    private static JsonElement ParseJsonElement(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
