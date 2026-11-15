using Sprechen.Mda.Contracts.V1.VoiceInterfaces;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Prüft, dass der Resolver das injizierte Auswahlverfahren mit normalisiertem Sprachcode
/// aufruft und dessen Profil übernimmt.
/// </summary>
public sealed class AcousticWakeWordDetectorConfigurationResolverProfileSelectionTests
{
    [Fact]
    public async Task ResolveAsync_InvokesInjectedSelector_WithCatalogProfilesAndNormalizedContext()
    {
        var fabricated = CreateFabricatedProfile();
        var selector = new RecordingProfileSelector(fabricated);
        var resolver = new AcousticWakeWordDetectorConfigurationResolver(selector);

        var inventory = new WakeWordInventory(
            activationForms: new List<string> { "hey sprechen" },
            recognitionOnlyActivationForms: new List<string>());

        var configuration = await resolver.ResolveAsync("EN", inventory, CancellationToken.None);

        Assert.Equal(1, selector.CallCount);
        Assert.NotNull(selector.LastProfiles);
        Assert.NotEmpty(selector.LastProfiles!);
        Assert.NotNull(selector.LastContext);
        Assert.Equal("en", selector.LastContext!.NormalizedIsoCode);
        Assert.Same(inventory, selector.LastContext.WakeWordInventory);

        Assert.Same(fabricated, configuration.DetectorProfile);
    }

    [Fact]
    public async Task ResolveAsync_WhenSelectorPicksDifferentProfile_ReturnsThatProfile()
    {
        var firstShadow = CreateFabricatedProfile("first");
        var secondShadow = CreateFabricatedProfile("second");

        var pickFirst = new AcousticWakeWordDetectorConfigurationResolver(
            new ConstantProfileSelector(firstShadow));
        var pickSecond = new AcousticWakeWordDetectorConfigurationResolver(
            new ConstantProfileSelector(secondShadow));

        var inventory = new WakeWordInventory(
            activationForms: new List<string> { "hey sprechen" },
            recognitionOnlyActivationForms: new List<string>());

        var firstResult = await pickFirst.ResolveAsync("en", inventory, CancellationToken.None);
        var secondResult = await pickSecond.ResolveAsync("en", inventory, CancellationToken.None);

        Assert.Same(firstShadow, firstResult.DetectorProfile);
        Assert.Same(secondShadow, secondResult.DetectorProfile);
        Assert.NotSame(firstResult.DetectorProfile, secondResult.DetectorProfile);
    }

    [Fact]
    public void Constructor_WithNullSelector_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => new AcousticWakeWordDetectorConfigurationResolver(null!));

        Assert.Equal("profileSelector", ex.ParamName);
    }

    private static WakeWordDetectorProfile CreateFabricatedProfile(string detectorId = "fabricated-profile")
    {
        return new WakeWordDetectorProfile
        {
            DetectorId = detectorId,
            OperatingMode = WakeWordDetectorOperatingMode.LexicalFallbackOnly,
            EligibilityPolicy = WakeWordDetectorEligibilityPolicy.DefaultInventoryOnly,
        };
    }
}
