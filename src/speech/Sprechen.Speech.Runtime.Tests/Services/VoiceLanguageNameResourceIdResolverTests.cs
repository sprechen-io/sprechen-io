using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class VoiceLanguageNameResourceIdResolverTests
{
    [Fact]
    public void Resolve_WithLanguageCode_ReturnsCanonicalResourceId()
    {
        var resolver = new VoiceLanguageNameResourceIdResolver();
        var language = LanguageCode.ParseIso6391("DE-de");

        Assert.Equal("voice.languageName.de", resolver.Resolve(language));
    }

    [Fact]
    public void Resolve_WithString_TrimsAndLowercases()
    {
        var resolver = new VoiceLanguageNameResourceIdResolver();

        // Der String-Aufruf akzeptiert auch Nicht-ISO-Kürzel; er trimmt und schreibt klein.
        Assert.Equal("voice.languageName.de", resolver.Resolve("  DE "));
        Assert.Equal("voice.languageName.xyz", resolver.Resolve("xYz"));
    }

    [Fact]
    public void Resolve_WithNull_ThrowsArgumentNullExceptionWithIsoCodeParamName()
    {
        var resolver = new VoiceLanguageNameResourceIdResolver();

        var exception = Assert.Throws<ArgumentNullException>(() => resolver.Resolve((string)null!));

        Assert.Equal("isoCode", exception.ParamName);
    }

    [Fact]
    public void Resolve_WithEmpty_ThrowsArgumentExceptionWithIsoCodeParamName()
    {
        var resolver = new VoiceLanguageNameResourceIdResolver();

        var exception = Assert.Throws<ArgumentException>(() => resolver.Resolve(string.Empty));

        Assert.Equal("isoCode", exception.ParamName);
        Assert.IsNotType<ArgumentNullException>(exception);
    }

    [Fact]
    public void Resolve_WithWhitespace_ThrowsArgumentExceptionWithIsoCodeParamName()
    {
        var resolver = new VoiceLanguageNameResourceIdResolver();

        var exception = Assert.Throws<ArgumentException>(() => resolver.Resolve("   "));

        Assert.Equal("isoCode", exception.ParamName);
        Assert.IsNotType<ArgumentNullException>(exception);
    }
}
