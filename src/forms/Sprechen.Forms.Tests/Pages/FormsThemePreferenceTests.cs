using Sprechen.Forms.Pages;
using Xunit;

namespace Sprechen.Forms.Tests.Pages;

public sealed class FormsThemePreferenceTests
{
    [Theory]
    [InlineData("light", "light")]
    [InlineData("dark", "dark")]
    [InlineData("system", "system")]
    [InlineData("DARK", "dark")]
    [InlineData("  Light  ", "light")]
    public void From_NormalizesSupportedValues(string raw, string expected)
    {
        Assert.Equal(expected, FormsThemePreference.From(raw).Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bogus")]
    public void From_FallsBackToSystem_ForUnknownOrEmpty(string? raw)
    {
        Assert.Equal(FormsThemePreference.System, FormsThemePreference.From(raw).Value);
    }

    [Fact]
    public void DataThemeAttribute_IsNullForSystem_SoTheMediaQueryGoverns()
    {
        Assert.Null(FormsThemePreference.From("system").DataThemeAttribute);
        Assert.Null(FormsThemePreference.From(null).DataThemeAttribute);
    }

    [Theory]
    [InlineData("light", "light")]
    [InlineData("dark", "dark")]
    public void DataThemeAttribute_EmitsExplicitChoice(string raw, string expected)
    {
        Assert.Equal(expected, FormsThemePreference.From(raw).DataThemeAttribute);
    }

    [Theory]
    [InlineData("system", true)]
    [InlineData("light", true)]
    [InlineData("dark", true)]
    [InlineData("DARK", true)]
    [InlineData(" light ", true)]
    [InlineData("bogus", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsSupported_AcceptsOnlyTheThreeCanonicalChoices(string? raw, bool expected)
    {
        Assert.Equal(expected, FormsThemePreference.IsSupported(raw));
    }

    [Fact]
    public void SupportedValues_AreSystemLightDarkInDisplayOrder()
    {
        Assert.Equal(new[] { "system", "light", "dark" }, FormsThemePreference.SupportedValues);
    }
}
