using Sprechen.Mda.Contracts.V1.Catalog;
using Xunit;

namespace Sprechen.Mda.Contracts.Tests;

public sealed class VoiceDialogSettingsTests
{
    [Fact]
    public void MergeWith_WhenFallbackIsNull_ReturnsSelf()
    {
        var settings = new VoiceDialogSettings
        {
            NoInputTimeoutMs = 5000,
            MaxRetries = 2
        };

        var result = settings.MergeWith(null);

        Assert.Same(settings, result);
    }

    [Fact]
    public void MergeWith_WhenAllValuesSet_UsesPrimaryValues()
    {
        var primary = new VoiceDialogSettings
        {
            NoInputTimeoutMs = 5000,
            MaxRetries = 2,
            ConfirmationPolicy = ConfirmationPolicy.Never,
            SttMinConfidenceToAccept = 90,
            SttMinConfidenceToConfirm = 70,
            SpeechRate = 1.5,
            PauseBetweenItemsMs = 200,
            CancelRefractoryMs = 3000,
            AllowBargeIn = true,
            PolitenessLevel = PolitenessLevel.Informal,
            ProgressCalculationBasis = ProgressCalculationBasis.RequiredFieldsOnly
        };

        var fallback = new VoiceDialogSettings
        {
            NoInputTimeoutMs = 10000,
            MaxRetries = 3,
            ConfirmationPolicy = ConfirmationPolicy.Always,
            SttMinConfidenceToAccept = 85,
            SttMinConfidenceToConfirm = 65,
            SpeechRate = 1.0,
            PauseBetweenItemsMs = 500,
            CancelRefractoryMs = 2000,
            AllowBargeIn = false,
            PolitenessLevel = PolitenessLevel.Formal,
            ProgressCalculationBasis = ProgressCalculationBasis.AllFields
        };

        var result = primary.MergeWith(fallback);

        Assert.Equal(5000, result.NoInputTimeoutMs);
        Assert.Equal(2, result.MaxRetries);
        Assert.Equal(ConfirmationPolicy.Never, result.ConfirmationPolicy);
        Assert.Equal(90, result.SttMinConfidenceToAccept);
        Assert.Equal(70, result.SttMinConfidenceToConfirm);
        Assert.Equal(1.5, result.SpeechRate);
        Assert.Equal(200, result.PauseBetweenItemsMs);
        Assert.Equal(3000, result.CancelRefractoryMs);
        Assert.True(result.AllowBargeIn.HasValue && result.AllowBargeIn.Value);
        Assert.Equal(PolitenessLevel.Informal, result.PolitenessLevel);
        Assert.Equal(ProgressCalculationBasis.RequiredFieldsOnly, result.ProgressCalculationBasis);
    }

    [Fact]
    public void MergeWith_WhenPrimaryValuesNull_UsesFallbackValues()
    {
        var primary = new VoiceDialogSettings();

        var fallback = new VoiceDialogSettings
        {
            NoInputTimeoutMs = 10000,
            MaxRetries = 3,
            ConfirmationPolicy = ConfirmationPolicy.Always,
            SttMinConfidenceToAccept = 85,
            SttMinConfidenceToConfirm = 65,
            SpeechRate = 1.0,
            PauseBetweenItemsMs = 500,
            CancelRefractoryMs = 2000,
            AllowBargeIn = true,
            PolitenessLevel = PolitenessLevel.Formal,
            ProgressCalculationBasis = ProgressCalculationBasis.AllFields
        };

        var result = primary.MergeWith(fallback);

        Assert.Equal(10000, result.NoInputTimeoutMs);
        Assert.Equal(3, result.MaxRetries);
        Assert.Equal(ConfirmationPolicy.Always, result.ConfirmationPolicy);
        Assert.Equal(85, result.SttMinConfidenceToAccept);
        Assert.Equal(65, result.SttMinConfidenceToConfirm);
        Assert.Equal(1.0, result.SpeechRate);
        Assert.Equal(500, result.PauseBetweenItemsMs);
        Assert.Equal(2000, result.CancelRefractoryMs);
        Assert.True(result.AllowBargeIn.HasValue && result.AllowBargeIn.Value);
        Assert.Equal(PolitenessLevel.Formal, result.PolitenessLevel);
        Assert.Equal(ProgressCalculationBasis.AllFields, result.ProgressCalculationBasis);
    }

    [Fact]
    public void MergeWith_WhenMixedValues_MergesCorrectly()
    {
        var primary = new VoiceDialogSettings
        {
            NoInputTimeoutMs = 5000,
            ConfirmationPolicy = ConfirmationPolicy.Ambiguous,
            SttMinConfidenceToAccept = 90,
            AllowBargeIn = false,
            PolitenessLevel = PolitenessLevel.Informal
        };

        var fallback = new VoiceDialogSettings
        {
            NoInputTimeoutMs = 10000,
            MaxRetries = 3,
            ConfirmationPolicy = ConfirmationPolicy.Always,
            SttMinConfidenceToAccept = 85,
            SttMinConfidenceToConfirm = 65,
            SpeechRate = 1.0,
            PauseBetweenItemsMs = 500,
            CancelRefractoryMs = 2500,
            AllowBargeIn = true,
            PolitenessLevel = PolitenessLevel.Formal,
            ProgressCalculationBasis = ProgressCalculationBasis.FieldGroupBased
        };

        var result = primary.MergeWith(fallback);

        Assert.Equal(5000, result.NoInputTimeoutMs);
        Assert.Equal(3, result.MaxRetries);
        Assert.Equal(ConfirmationPolicy.Ambiguous, result.ConfirmationPolicy);
        Assert.Equal(90, result.SttMinConfidenceToAccept);
        Assert.Equal(65, result.SttMinConfidenceToConfirm);
        Assert.Equal(1.0, result.SpeechRate);
        Assert.Equal(500, result.PauseBetweenItemsMs);
        Assert.Equal(2500, result.CancelRefractoryMs);
        Assert.True(result.AllowBargeIn.HasValue && !result.AllowBargeIn.Value);
        Assert.Equal(PolitenessLevel.Informal, result.PolitenessLevel);
        Assert.Equal(ProgressCalculationBasis.FieldGroupBased, result.ProgressCalculationBasis);
    }

    [Fact]
    public void GetNoInputTimeoutMs_WhenNotSet_ReturnsDefault()
    {
        var settings = new VoiceDialogSettings();

        Assert.Equal(10000, settings.GetNoInputTimeoutMs());
        Assert.Equal(5000, settings.GetNoInputTimeoutMs(5000));
    }

    [Fact]
    public void GetNoInputTimeoutMs_WhenSet_ReturnsValue()
    {
        var settings = new VoiceDialogSettings { NoInputTimeoutMs = 8000 };

        Assert.Equal(8000, settings.GetNoInputTimeoutMs());
        Assert.Equal(8000, settings.GetNoInputTimeoutMs(5000));
    }

    [Fact]
    public void GetMaxRetries_WhenNotSet_ReturnsDefault()
    {
        var settings = new VoiceDialogSettings();

        Assert.Equal(3, settings.GetMaxRetries());
        Assert.Equal(5, settings.GetMaxRetries(5));
    }

    [Fact]
    public void GetMaxRetries_WhenSet_ReturnsValue()
    {
        var settings = new VoiceDialogSettings { MaxRetries = 2 };

        Assert.Equal(2, settings.GetMaxRetries());
        Assert.Equal(2, settings.GetMaxRetries(5));
    }

    [Fact]
    public void GetConfirmationPolicy_WhenNotSet_ReturnsDefault()
    {
        var settings = new VoiceDialogSettings();

        Assert.Equal(ConfirmationPolicy.Always, settings.GetConfirmationPolicy());
        Assert.Equal(ConfirmationPolicy.Never, settings.GetConfirmationPolicy(ConfirmationPolicy.Never));
    }

    [Fact]
    public void GetConfirmationPolicy_WhenSet_ReturnsValue()
    {
        var settings = new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Ambiguous };

        Assert.Equal(ConfirmationPolicy.Ambiguous, settings.GetConfirmationPolicy());
        Assert.Equal(ConfirmationPolicy.Ambiguous, settings.GetConfirmationPolicy(ConfirmationPolicy.Never));
    }

    [Fact]
    public void GetSttMinConfidenceToAccept_WhenNotSet_ReturnsCanonicalDefault()
    {
        var settings = new VoiceDialogSettings();

        Assert.Equal(
            CatalogVoiceDialogSettingBounds.SttMinConfidenceToAcceptDefault,
            settings.GetSttMinConfidenceToAccept());
        Assert.Equal(90, settings.GetSttMinConfidenceToAccept(90));
    }

    [Fact]
    public void GetSttMinConfidenceToAccept_WhenSet_ReturnsValue()
    {
        var settings = new VoiceDialogSettings { SttMinConfidenceToAccept = 91 };

        Assert.Equal(91, settings.GetSttMinConfidenceToAccept());
        Assert.Equal(91, settings.GetSttMinConfidenceToAccept(90));
    }

    [Fact]
    public void GetSttMinConfidenceToConfirm_WhenNotSet_ReturnsCanonicalDefault()
    {
        var settings = new VoiceDialogSettings();

        Assert.Equal(
            CatalogVoiceDialogSettingBounds.SttMinConfidenceToConfirmDefault,
            settings.GetSttMinConfidenceToConfirm());
        Assert.Equal(70, settings.GetSttMinConfidenceToConfirm(70));
    }

    [Fact]
    public void GetSttMinConfidenceToConfirm_WhenSet_ReturnsValue()
    {
        var settings = new VoiceDialogSettings { SttMinConfidenceToConfirm = 71 };

        Assert.Equal(71, settings.GetSttMinConfidenceToConfirm());
        Assert.Equal(71, settings.GetSttMinConfidenceToConfirm(70));
    }

    [Fact]
    public void GetSpeechRate_WhenNotSet_ReturnsDefault()
    {
        var settings = new VoiceDialogSettings();

        Assert.Equal(1.0, settings.GetSpeechRate());
        Assert.Equal(1.5, settings.GetSpeechRate(1.5));
    }

    [Fact]
    public void GetSpeechRate_WhenSet_ReturnsValue()
    {
        var settings = new VoiceDialogSettings { SpeechRate = 0.8 };

        Assert.Equal(0.8, settings.GetSpeechRate());
        Assert.Equal(0.8, settings.GetSpeechRate(1.5));
    }

    [Fact]
    public void GetPauseBetweenItemsMs_WhenNotSet_ReturnsDefault()
    {
        var settings = new VoiceDialogSettings();

        Assert.Equal(500, settings.GetPauseBetweenItemsMs());
        Assert.Equal(1000, settings.GetPauseBetweenItemsMs(1000));
    }

    [Fact]
    public void GetPauseBetweenItemsMs_WhenSet_ReturnsValue()
    {
        var settings = new VoiceDialogSettings { PauseBetweenItemsMs = 300 };

        Assert.Equal(300, settings.GetPauseBetweenItemsMs());
        Assert.Equal(300, settings.GetPauseBetweenItemsMs(1000));
    }

    [Fact]
    public void GetCancelRefractoryMs_WhenNotSet_ReturnsDefault()
    {
        var settings = new VoiceDialogSettings();

        Assert.Equal(2000, settings.GetCancelRefractoryMs());
        Assert.Equal(500, settings.GetCancelRefractoryMs(500));
    }

    [Fact]
    public void GetCancelRefractoryMs_WhenSet_ReturnsValue()
    {
        var settings = new VoiceDialogSettings { CancelRefractoryMs = 300 };

        Assert.Equal(300, settings.GetCancelRefractoryMs());
        Assert.Equal(300, settings.GetCancelRefractoryMs(1000));
    }

    [Fact]
    public void GetCancelRefractoryDuration_WhenSet_ReturnsDuration()
    {
        var settings = new VoiceDialogSettings { CancelRefractoryMs = 750 };

        Assert.Equal(TimeSpan.FromMilliseconds(750), settings.GetCancelRefractoryDuration());
    }

    [Fact]
    public void GetAllowBargeIn_WhenNotSet_ReturnsDefaultFalse()
    {
        var settings = new VoiceDialogSettings();

        Assert.False(settings.GetAllowBargeIn());
        Assert.True(settings.GetAllowBargeIn(defaultValue: true));
    }

    [Fact]
    public void GetAllowBargeIn_WhenSet_ReturnsValue()
    {
        var settings = new VoiceDialogSettings { AllowBargeIn = true };

        Assert.True(settings.GetAllowBargeIn());
        Assert.True(settings.GetAllowBargeIn(defaultValue: false));
    }

    [Fact]
    public void GetAutoAdvanceAfterCommit_WhenNotSet_ReturnsDefaultTrue()
    {
        var settings = new VoiceDialogSettings();

        Assert.True(settings.GetAutoAdvanceAfterCommit());
        Assert.False(settings.GetAutoAdvanceAfterCommit(defaultValue: false));
    }

    [Fact]
    public void GetAutoAdvanceAfterCommit_WhenSet_ReturnsValue()
    {
        var settings = new VoiceDialogSettings { AutoAdvanceAfterCommit = false };

        Assert.False(settings.GetAutoAdvanceAfterCommit());
        Assert.False(settings.GetAutoAdvanceAfterCommit(defaultValue: true));
    }

    [Fact]
    public void MergeWith_CarriesAutoAdvanceAfterCommit()
    {
        var primary = new VoiceDialogSettings { AutoAdvanceAfterCommit = false };
        var fallback = new VoiceDialogSettings { AutoAdvanceAfterCommit = true };

        var primaryWins = primary.MergeWith(fallback);
        var fallbackWins = new VoiceDialogSettings().MergeWith(fallback);

        Assert.False(primaryWins.AutoAdvanceAfterCommit);
        Assert.True(fallbackWins.AutoAdvanceAfterCommit);
    }

    [Fact]
    public void GetPolitenessLevel_WhenNotSet_ReturnsDefault()
    {
        var settings = new VoiceDialogSettings();

        Assert.Equal(PolitenessLevel.Formal, settings.GetPolitenessLevel());
        Assert.Equal(PolitenessLevel.Informal, settings.GetPolitenessLevel(PolitenessLevel.Informal));
    }

    [Fact]
    public void GetPolitenessLevel_WhenSet_ReturnsValue()
    {
        var settings = new VoiceDialogSettings { PolitenessLevel = PolitenessLevel.Informal };

        Assert.Equal(PolitenessLevel.Informal, settings.GetPolitenessLevel());
        Assert.Equal(PolitenessLevel.Informal, settings.GetPolitenessLevel(PolitenessLevel.Formal));
    }

    [Fact]
    public void GetProgressCalculationBasis_WhenNotSet_ReturnsDefault()
    {
        var settings = new VoiceDialogSettings();

        Assert.Equal(ProgressCalculationBasis.AllFields, settings.GetProgressCalculationBasis());
        Assert.Equal(
            ProgressCalculationBasis.RequiredFieldsOnly,
            settings.GetProgressCalculationBasis(ProgressCalculationBasis.RequiredFieldsOnly));
    }

    [Fact]
    public void GetProgressCalculationBasis_WhenSet_ReturnsValue()
    {
        var settings = new VoiceDialogSettings { ProgressCalculationBasis = ProgressCalculationBasis.FieldGroupBased };

        Assert.Equal(ProgressCalculationBasis.FieldGroupBased, settings.GetProgressCalculationBasis());
        Assert.Equal(
            ProgressCalculationBasis.FieldGroupBased,
            settings.GetProgressCalculationBasis(ProgressCalculationBasis.AllFields));
    }
}
