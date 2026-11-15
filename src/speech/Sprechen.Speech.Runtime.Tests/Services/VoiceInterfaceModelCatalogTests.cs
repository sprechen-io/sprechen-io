using System.Text.Json;
using System.Text.Json.Serialization;
using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;
using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class VoiceInterfaceModelCatalogTests
{

    [Fact]
    public void HasResource_WhenVoiceRuntimeResourceIdIsKnown_ReturnsTrue()
    {
        Assert.True(VoiceInterfaceModelCatalog.Resources.HasResource(VoiceInterfaceResourceIds.SessionSetupWakeWordsPrompt));
    }

    [Fact]
    public void TryGetResourceText_WhenRequiredResourceExists_ReturnsModeledText()
    {
        var found = VoiceInterfaceModelCatalog.Resources.TryGetResourceText(
            VoiceInterfaceResourceIds.SessionSetupWakeWordsPrompt,
            "de",
            out var text);

        Assert.True(found);
        Assert.False(string.IsNullOrWhiteSpace(text));
    }

    [Fact]
    public void GetWakeWordActivationForms_ReturnModeledInventories()
    {
        Assert.Equal(["okay alex", "hey alex"], VoiceInterfaceModelCatalog.WakeWord.GetActivationFormsForLanguage("de"));
        Assert.Equal(["ok alex"], VoiceInterfaceModelCatalog.WakeWord.GetRecognitionOnlyActivationFormsForLanguage("de"));
        Assert.Equal(["okay alex", "hey alex"], VoiceInterfaceModelCatalog.WakeWord.GetActivationFormsForLanguage("es"));
        Assert.Equal(["okay alex", "hey alex"], VoiceInterfaceModelCatalog.WakeWord.GetActivationFormsForLanguage("it"));
    }

    [Fact]
    public void GetWakeWordRecoveryNotRecognizedEarconId_ReferenceContractValueParsesToCanonicalEarconId()
    {
        var earconId = VoiceInterfaceModelCatalog.WakeWord.GetRecoveryNotRecognizedEarconId();

        Assert.NotNull(earconId);
        Assert.Equal(EarconId.WakeWordNotRecognized, earconId);
    }

    // Die erfolgreiche Aktivierung soll ihr eigenes modelliertes Bestätigungssignal behalten.
    [Fact]
    public void GetActivationEarconId_ReferenceContractValueParsesToCanonicalEarconId()
    {
        var earconId = VoiceInterfaceModelCatalog.WakeWord.GetActivationEarconId();

        Assert.NotNull(earconId);
        Assert.Equal(EarconId.FlowAck, earconId);
    }

    [Fact]
    public void GetWakeUtteranceWindow_ReturnsModeledValuesFromReferenceContract()
    {
        var window = VoiceInterfaceModelCatalog.WakeWord.GetUtteranceWindow();

        Assert.NotNull(window);
        Assert.Equal(1500, window!.MaxInterCyclePauseMs);
        Assert.Equal(1, window.MaxCarriedCycles);
        Assert.True(window.PrefixOnlyFirstCycleAccepted);
    }

    [Fact]
    public void GetWakeWordDetectorCatalog_ReturnsModeledReferenceContract()
    {
        var catalog = VoiceInterfaceModelCatalog.WakeWord.DetectorCatalog;

        Assert.Equal("default.voiceWakeWordDetectors", catalog.DetectorCatalogModelId);
        Assert.Equal("default.voiceWakeWords", catalog.WakeWordModelId);
        var profile = Assert.Single(catalog.DetectorProfiles);
        Assert.Equal("default.okay.detector", profile.DetectorId);
        Assert.Equal(WakeWordDetectorOperatingMode.LexicalFallbackOnly, profile.OperatingMode);
    }
}
