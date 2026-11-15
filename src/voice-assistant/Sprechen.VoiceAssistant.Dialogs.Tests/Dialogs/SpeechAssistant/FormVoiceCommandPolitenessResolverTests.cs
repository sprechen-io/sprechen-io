using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandPolitenessResolverTests
{
    private readonly FormVoiceCommandPolitenessResolver _sut = new();

    [Fact]
    public void TryResolve_WhenGermanFormalWordIsRequested_ReturnsFormal()
    {
        var resolved = _sut.TryResolve("sie", "de", out var politenessLevel, out var displayLabel);

        Assert.True(resolved);
        Assert.Equal(PolitenessLevel.Formal, politenessLevel);
        Assert.Equal("sie", displayLabel);
    }

    [Fact]
    public void TryResolve_WhenLeadingNoiseExists_ReturnsInformal()
    {
        var resolved = _sut.TryResolve("to informal", "en", out var politenessLevel, out var displayLabel);

        Assert.True(resolved);
        Assert.Equal(PolitenessLevel.Informal, politenessLevel);
        Assert.Equal("informal", displayLabel);
    }

    [Fact]
    public void TryResolve_WhenLanguageHasNoPolitenessProfile_ReturnsFalse()
    {
        var resolved = _sut.TryResolve("formal", "pt", out var politenessLevel, out var displayLabel);

        Assert.False(resolved);
        Assert.Equal(default, politenessLevel);
        Assert.Equal(string.Empty, displayLabel);
    }
}
