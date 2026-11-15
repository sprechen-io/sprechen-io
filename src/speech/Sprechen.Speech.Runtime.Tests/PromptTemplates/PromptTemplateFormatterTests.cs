using Sprechen.Speech.App.Contracts.PromptTemplates;
using Xunit;

namespace Sprechen.Speech.Runtime.Tests.PromptTemplates;

public sealed class PromptTemplateFormatterTests
{
    [Fact]
    public void Apply_PresentKeys_AreSubstituted()
    {
        var result = PromptTemplateFormatter.Apply(
            "Hello {{name}}",
            new Dictionary<string, string?> { ["name"] = "World" });

        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void Apply_FullyPopulatedTemplate_IsUnchanged()
    {
        var result = PromptTemplateFormatter.Apply(
            "On {{form}}, field {{field}}.",
            new Dictionary<string, string?> { ["form"] = "Order", ["field"] = "Name" });

        Assert.Equal("On Order, field Name.", result);
    }

    [Fact]
    public void Apply_NullParameterValue_BecomesEmptyAndTidiesOrphanPeriod()
    {
        var result = PromptTemplateFormatter.Apply(
            "Aktuelles Feld {{currentField}}.",
            new Dictionary<string, string?> { ["currentField"] = null });

        Assert.Equal("Aktuelles Feld.", result);
    }

    [Fact]
    public void Apply_UnresolvedToken_IsStripped()
    {
        var result = PromptTemplateFormatter.Apply(
            "Hello {{missing}} World",
            new Dictionary<string, string?> { ["present"] = "ignored" });

        Assert.Equal("Hello World", result);
        Assert.DoesNotContain("{{", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Apply_StrayTokenWithoutParameters_IsStripped()
    {
        var result = PromptTemplateFormatter.Apply("Value {{leftover}} here", templateParameters: null);

        Assert.Equal("Value here", result);
        Assert.DoesNotContain("{{", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Apply_OrphanCommaAndDoubleSpace_AreTidied()
    {
        var result = PromptTemplateFormatter.Apply(
            "Wert {{value}} , Rest",
            new Dictionary<string, string?> { ["value"] = string.Empty });

        Assert.Equal("Wert, Rest", result);
    }

    [Fact]
    public void Apply_MultipleSpaces_AreCollapsed()
    {
        var result = PromptTemplateFormatter.Apply("a    b", templateParameters: null);

        Assert.Equal("a b", result);
    }

    [Fact]
    public void Apply_EmptyTemplate_IsReturnedUnchanged()
    {
        Assert.Equal(string.Empty, PromptTemplateFormatter.Apply(string.Empty, templateParameters: null));
    }
}
