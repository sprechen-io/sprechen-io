using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Mda.Contracts.Tests.FormContracts;

public sealed class HtmlProjectionProfileResolverTests
{
    [Fact]
    public void Resolve_NullProjection_ReturnsA11yFirst()
    {
        var resolved = HtmlProjectionProfileResolver.Resolve(null);

        Assert.Equal(HtmlProjectionProfiles.A11yFirst, resolved);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_MissingOrWhitespaceProfile_ReturnsA11yFirst(string? profile)
    {
        var projection = new HtmlProjection { Profile = profile };

        var resolved = HtmlProjectionProfileResolver.Resolve(projection);

        Assert.Equal(HtmlProjectionProfiles.A11yFirst, resolved);
    }

    [Fact]
    public void Resolve_SupportedProfile_ReturnsA11yFirst()
    {
        var projection = new HtmlProjection { Profile = HtmlProjectionProfiles.A11yFirst };

        var resolved = HtmlProjectionProfileResolver.Resolve(projection);

        Assert.Equal(HtmlProjectionProfiles.A11yFirst, resolved);
    }

    [Theory]
    [InlineData("A11yFirst")]
    [InlineData("highContrast")]
    [InlineData("default")]
    public void Resolve_UnsupportedProfile_ThrowsWithStableErrorCode(string profile)
    {
        var projection = new HtmlProjection { Profile = profile };

        var ex = Assert.Throws<InvalidOperationException>(
            () => HtmlProjectionProfileResolver.Resolve(projection));
        Assert.Contains(HtmlProjectionProfileResolver.ErrorCodeUnsupportedProfile, ex.Message);
        Assert.Contains(profile, ex.Message);
    }
}
