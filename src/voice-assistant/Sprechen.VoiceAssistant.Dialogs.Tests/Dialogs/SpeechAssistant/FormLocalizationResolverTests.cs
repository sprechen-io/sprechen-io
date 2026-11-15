using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormLocalizationResolverTests
{
    private const string DefaultLanguage = "de";
    private static readonly ImmutableArray<string> SupportedLanguages = ["de", "en", "fr"];

    #region ResolveText

    [Fact]
    public void ResolveText_WhenTextIsNull_ReturnsFallback()
    {
        var resolver = CreateResolver("de");

        var result = resolver.ResolveText(null, fallback: "fallback text");

        Assert.Equal("fallback text", result);
    }

    [Fact]
    public void ResolveText_WhenTextIsNullAndFallbackIsNull_ReturnsEmptyString()
    {
        var resolver = CreateResolver("de");

        var result = resolver.ResolveText(null, fallback: null);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ResolveText_WhenRequestedLanguageExists_ReturnsRequestedLanguage()
    {
        var resolver = CreateResolver("de");
        var text = CreateLocalizedText("Hallo", "Hello");

        var result = resolver.ResolveText(text, fallback: "default");

        Assert.Equal("Hallo", result);
    }

    [Fact]
    public void ResolveText_WhenRequestedLanguageNotExists_ReturnsDefaultLanguage()
    {
        var resolver = CreateResolver("fr");
        var text = CreateLocalizedText("Hallo", "Hello");

        var result = resolver.ResolveText(text, fallback: "default");

        Assert.Equal("Hallo", result); // Rückfall auf die Standardsprache Deutsch.
    }

    [Fact]
    public void ResolveText_WhenNoMatchingLanguage_ReturnsFallback()
    {
        var resolver = CreateResolver("it", strictLocalization: true);
        var text = CreateLocalizedText(de: null, en: null); // Leere Sammlung lokalisierter Texte.

        var result = resolver.ResolveText(text, fallback: "fallback");

        Assert.Equal("fallback", result);
    }

    [Fact]
    public void ResolveText_WhenTextIsEmpty_ReturnsFallback()
    {
        var resolver = CreateResolver("de");
        var text = CreateLocalizedText("", "");

        var result = resolver.ResolveText(text, fallback: "fallback");

        Assert.Equal("fallback", result);
    }

    [Fact]
    public void ResolveText_WhenTextHasWhitespace_ReturnsTrimed()
    {
        var resolver = CreateResolver("de");
        var text = CreateLocalizedText("  Hallo  ", "Hello");

        var result = resolver.ResolveText(text, fallback: "default");

        Assert.Equal("Hallo", result);
    }

    #endregion

    #region ResolveChoiceOptions

    [Fact]
    public void ResolveChoiceOptions_WhenVariableNotFound_ReturnsEmpty()
    {
        var variables = CreateVariables();
        var resolver = CreateResolver("de", variables);

        var result = resolver.ResolveChoiceOptions("nonExistentVar");

        Assert.Empty(result);
    }

    [Fact]
    public void ResolveChoiceOptions_WhenValueTypeIsNotObject_ReturnsEmpty()
    {
        var variables = CreateVariables(("testVar", CreateSimpleVariable()));
        var resolver = CreateResolver("de", variables);

        var result = resolver.ResolveChoiceOptions("testVar");

        Assert.Empty(result);
    }

    [Fact]
    public void ResolveChoiceOptions_WhenValuesIsMissing_ReturnsEmpty()
    {
        var valueType = JsonDocument.Parse("""{ "kind": "enum" }""").RootElement;
        var variables = CreateVariables(("testVar", CreateVariableWithValueType(valueType)));
        var resolver = CreateResolver("de", variables);

        var result = resolver.ResolveChoiceOptions("testVar");

        Assert.Empty(result);
    }

    [Fact]
    public void ResolveChoiceOptions_WhenValuesIsNotArray_ReturnsEmpty()
    {
        var valueType = JsonDocument.Parse("""{ "kind": "enum", "values": "not-an-array" }""").RootElement;
        var variables = CreateVariables(("testVar", CreateVariableWithValueType(valueType)));
        var resolver = CreateResolver("de", variables);

        var result = resolver.ResolveChoiceOptions("testVar");

        Assert.Empty(result);
    }

    [Fact]
    public void ResolveChoiceOptions_WhenCodeIsMissing_SkipsValue()
    {
        var valueType = JsonDocument.Parse("""
        {
            "kind": "enum",
            "values": [
                { "displayName": { "de": "Test" } }
            ]
        }
        """).RootElement;
        var variables = CreateVariables(("testVar", CreateVariableWithValueType(valueType)));
        var resolver = CreateResolver("de", variables);

        var result = resolver.ResolveChoiceOptions("testVar");

        Assert.Empty(result);
    }

    [Fact]
    public void ResolveChoiceOptions_WhenCodeIsEmpty_SkipsValue()
    {
        var valueType = JsonDocument.Parse("""
        {
            "kind": "enum",
            "values": [
                { "code": "", "displayName": { "de": "Test" } }
            ]
        }
        """).RootElement;
        var variables = CreateVariables(("testVar", CreateVariableWithValueType(valueType)));
        var resolver = CreateResolver("de", variables);

        var result = resolver.ResolveChoiceOptions("testVar");

        Assert.Empty(result);
    }

    [Fact]
    public void ResolveChoiceOptions_WhenValidCode_ReturnsOption()
    {
        var valueType = JsonDocument.Parse("""
        {
            "kind": "enum",
            "values": [
                { "code": "opt1", "displayName": { "de": "Option Eins", "en": "Option One" } },
                { "code": "opt2", "displayName": { "de": "Option Zwei", "en": "Option Two" } }
            ]
        }
        """).RootElement;
        var variables = CreateVariables(("testVar", CreateVariableWithValueType(valueType)));
        var resolver = CreateResolver("de", variables);

        var result = resolver.ResolveChoiceOptions("testVar");

        Assert.Equal(2, result.Length);
        Assert.Equal("opt1", result[0].Code);
        Assert.Equal("Option Eins", result[0].DisplayName);
        Assert.Equal("opt2", result[1].Code);
        Assert.Equal("Option Zwei", result[1].DisplayName);
    }

    [Fact]
    public void ResolveChoiceOptions_WhenNoDisplayName_UsesCode()
    {
        var valueType = JsonDocument.Parse("""
        {
            "kind": "enum",
            "values": [
                { "code": "myCode" }
            ]
        }
        """).RootElement;
        var variables = CreateVariables(("testVar", CreateVariableWithValueType(valueType)));
        var resolver = CreateResolver("de", variables);

        var result = resolver.ResolveChoiceOptions("testVar");

        Assert.Single(result);
        Assert.Equal("myCode", result[0].Code);
        Assert.Equal("myCode", result[0].DisplayName);
    }

    [Fact]
    public void ResolveChoiceOptions_UsesRequestedLanguage()
    {
        var valueType = JsonDocument.Parse("""
        {
            "kind": "enum",
            "values": [
                { "code": "opt1", "displayName": { "de": "Deutsch", "en": "English" } }
            ]
        }
        """).RootElement;
        var variables = CreateVariables(("testVar", CreateVariableWithValueType(valueType)));
        var resolver = CreateResolver("en", variables);

        var result = resolver.ResolveChoiceOptions("testVar");

        Assert.Single(result);
        Assert.Equal("English", result[0].DisplayName);
    }

    [Fact]
    public void ResolveChoiceOptions_TrimsCode()
    {
        var valueType = JsonDocument.Parse("""
        {
            "kind": "enum",
            "values": [
                { "code": "  spacedCode  " }
            ]
        }
        """).RootElement;
        var variables = CreateVariables(("testVar", CreateVariableWithValueType(valueType)));
        var resolver = CreateResolver("de", variables);

        var result = resolver.ResolveChoiceOptions("testVar");

        Assert.Single(result);
        Assert.Equal("spacedCode", result[0].Code);
    }

    #endregion

    #region ResolveFieldGroupTitle

    [Fact]
    public void ResolveFieldGroupTitle_WhenFieldGroupIsUnknown_ReturnsFieldGroupId()
    {
        var fieldGroups = CreateFieldGroups();
        var resolver = CreateResolver("de", fieldGroups: fieldGroups);

        var result = resolver.ResolveFieldGroupTitle("nonExistentFieldGroup");

        Assert.Equal("nonExistentFieldGroup", result);
    }

    [Fact]
    public void ResolveFieldGroupTitle_WhenFieldGroupExists_ReturnsLocalizedTitle()
    {
        var fieldGroups = CreateFieldGroups(("fieldGroup1", CreateFieldGroup("Feldgruppe Eins", "Field Group One")));
        var resolver = CreateResolver("de", fieldGroups: fieldGroups);

        var result = resolver.ResolveFieldGroupTitle("fieldGroup1");

        Assert.Equal("Feldgruppe Eins", result);
    }

    [Fact]
    public void ResolveFieldGroupTitle_UsesRequestedLanguage()
    {
        var fieldGroups = CreateFieldGroups(("fieldGroup1", CreateFieldGroup("Feldgruppe Eins", "Field Group One")));
        var resolver = CreateResolver("en", fieldGroups: fieldGroups);

        var result = resolver.ResolveFieldGroupTitle("fieldGroup1");

        Assert.Equal("Field Group One", result);
    }

    #endregion

    #region Test Helpers

    private static FormLocalizationResolver CreateResolver(
        string requestedIsoCode,
        IReadOnlyDictionary<string, VariableDefinition>? variables = null,
        IReadOnlyDictionary<string, FieldGroupDefinition>? fieldGroups = null,
        bool strictLocalization = false)
    {
        return new FormLocalizationResolver(
            requestedIsoCode,
            DefaultLanguage,
            SupportedLanguages,
            strictLocalization,
            Sprechen.Mda.Contracts.V1.Catalog.PolitenessLevel.Formal,
            (variables ?? CreateVariables()).ToImmutableDictionary(kvp => VariableId.From(kvp.Key), kvp => kvp.Value),
            (fieldGroups ?? CreateFieldGroups()).ToImmutableDictionary(kvp => FieldGroupId.From(kvp.Key), kvp => kvp.Value));
    }

    private static LocalizedText CreateLocalizedText(string? de, string? en)
    {
        var text = new LocalizedText();
        if (de != null)
        {
            text["de"] = de;
        }
        if (en != null)
        {
            text["en"] = en;
        }
        return text;
    }

    private static IReadOnlyDictionary<string, VariableDefinition> CreateVariables(
        params (string variableId, VariableDefinition variable)[] entries)
    {
        var dict = new Dictionary<string, VariableDefinition>(StringComparer.Ordinal);
        foreach (var (variableId, variable) in entries)
        {
            dict[variableId] = variable;
        }
        return dict;
    }

    private static VariableDefinition CreateSimpleVariable()
    {
        return new VariableDefinition
        {
            VariableId = VariableId.From("testVar"),
            ValueType = JsonDocument.Parse("\"string\"").RootElement
        };
    }

    private static VariableDefinition CreateVariableWithValueType(JsonElement valueType)
    {
        return new VariableDefinition
        {
            VariableId = VariableId.From("testVar"),
            ValueType = valueType
        };
    }

    private static IReadOnlyDictionary<string, FieldGroupDefinition> CreateFieldGroups(
        params (string fieldGroupId, FieldGroupDefinition fieldGroup)[] entries)
    {
        var dict = new Dictionary<string, FieldGroupDefinition>(StringComparer.Ordinal);
        foreach (var (fieldGroupId, fieldGroup) in entries)
        {
            dict[fieldGroupId] = fieldGroup;
        }
        return dict;
    }

    private static FieldGroupDefinition CreateFieldGroup(string de, string en)
    {
        return new FieldGroupDefinition
        {
            FieldGroupId = FieldGroupId.From("fieldGroup1"),
            Title = new LocalizedText { ["de"] = de, ["en"] = en }
        };
    }

    #endregion
}



