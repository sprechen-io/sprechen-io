using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class FirstProfileSelectorTests
{
    [Fact]
    public void Select_WithSingleProfile_ReturnsThatProfile()
    {
        var selector = new FirstProfileSelector();
        var only = CreateProfile("only");
        var profiles = new List<WakeWordDetectorProfile> { only };

        var result = selector.Select(profiles, CreateContext());

        Assert.Same(only, result);
    }

    [Fact]
    public void Select_WithMultipleProfiles_ReturnsFirstProfile()
    {
        var selector = new FirstProfileSelector();
        var first = CreateProfile("first");
        var second = CreateProfile("second");
        var third = CreateProfile("third");
        var profiles = new List<WakeWordDetectorProfile> { first, second, third };

        var result = selector.Select(profiles, CreateContext());

        Assert.Same(first, result);
    }

    [Fact]
    public void Select_WithEmptyProfileList_FailsFastWithArgumentException()
    {
        var selector = new FirstProfileSelector();
        var profiles = new List<WakeWordDetectorProfile>();

        var ex = Assert.Throws<ArgumentException>(() => selector.Select(profiles, CreateContext()));
        Assert.Equal("profiles", ex.ParamName);
    }

    [Fact]
    public void Select_WithNullProfiles_Throws()
    {
        var selector = new FirstProfileSelector();

        var ex = Assert.Throws<ArgumentNullException>(() => selector.Select(null!, CreateContext()));
        Assert.Equal("profiles", ex.ParamName);
    }

    [Fact]
    public void Select_WithNullContext_Throws()
    {
        var selector = new FirstProfileSelector();
        var profiles = new List<WakeWordDetectorProfile> { CreateProfile("any") };

        var ex = Assert.Throws<ArgumentNullException>(() => selector.Select(profiles, null!));
        Assert.Equal("context", ex.ParamName);
    }

    private static WakeWordDetectorProfile CreateProfile(string detectorId)
    {
        return new WakeWordDetectorProfile
        {
            DetectorId = detectorId,
            OperatingMode = WakeWordDetectorOperatingMode.LexicalFallbackOnly,
            EligibilityPolicy = WakeWordDetectorEligibilityPolicy.DefaultInventoryOnly,
        };
    }

    private static WakeWordDetectorProfileSelectionContext CreateContext()
    {
        var inventory = new WakeWordInventory(
            activationForms: new List<string> { "hey sprechen" },
            recognitionOnlyActivationForms: new List<string>());
        return new WakeWordDetectorProfileSelectionContext("en", inventory);
    }
}
