using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.VoiceInterfaces;
using Sprechen.Speech.App.ClientInfrastructure.Services;
using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

namespace Sprechen.Speech.Runtime.Tests.Schema;

public sealed class WakeWordCatalogReferenceAlignmentTests
{

    [Fact]
    public void MicrophoneAvailabilityPolicy_IsSessionWideAutomaticAndAudiblyRecovers()
    {
        var settings = VoiceInterfaceModelCatalog.WakeWord.GetMicrophoneAvailabilityPolicySettings();

        Assert.NotNull(settings);
        Assert.Equal(2, settings!.FailureThreshold);
        Assert.Equal(60, settings.CooldownSeconds);
        Assert.Equal(300, settings.MaxCooldownSeconds);
        Assert.True(settings.AutomaticRetry);
        Assert.True(settings.AnnounceRecovery);
        Assert.True(VoiceInterfaceModelCatalog.Resources.HasResource(
            VoiceInterfaceResourceIds.SpeechCaptureRecoveryMicrophoneAvailable));
    }

    [Theory]
    [InlineData("de", "automatisch", "pausieren")]
    [InlineData("en", "automatically", "pause")]
    [InlineData("fr", "automatiquement", "pause")]
    [InlineData("es", "autom\u00e1ticamente", "pausar")]
    [InlineData("it", "automaticamente", "pausa")]
    public void ProductionMicrophoneGuidance_ComposesModeledPolicyAndAudibleExit(
        string isoCode,
        string retryMarker,
        string pauseMarker)
    {
        foreach (var politenessLevel in new[] { PolitenessLevel.Formal, PolitenessLevel.Informal })
        {
            var guidance = SpeechAssistantWakeWordRuntime.BuildMicrophoneAvailabilityGuidance(
                isoCode,
                politenessLevel);

            Assert.Equal(isoCode, guidance.IsoCode);
            Assert.Equal(2, guidance.UnavailableThreshold);
            Assert.Equal(TimeSpan.FromSeconds(60), guidance.Cooldown);
            Assert.Equal(TimeSpan.FromSeconds(300), guidance.MaxCooldown);
            foreach (var text in new[]
                     {
                         guidance.DeviceNotFoundText,
                         guidance.PermissionDeniedText,
                         guidance.UnavailableText,
                     })
            {
                Assert.Contains(retryMarker, text, StringComparison.OrdinalIgnoreCase);
                Assert.Contains(pauseMarker, text, StringComparison.OrdinalIgnoreCase);
            }

            Assert.False(string.IsNullOrWhiteSpace(guidance.RecoveryText));
        }
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void WakeWordEffectiveForms_AreAlexWithOkayAndHeyActivationAndOkRecognitionOnly(string isoCode)
    {
        // Die Reihenfolge der Aktivierungsformen folgt dem Katalog: okay vor hey.
        Assert.Equal(
            ["okay alex", "hey alex"],
            VoiceInterfaceModelCatalog.WakeWord.GetActivationFormsForLanguage(isoCode));
        Assert.Equal(
            ["ok alex"],
            VoiceInterfaceModelCatalog.WakeWord.GetRecognitionOnlyActivationFormsForLanguage(isoCode));
    }
}
