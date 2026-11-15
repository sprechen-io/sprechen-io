using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormVoice;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class VoiceDialogSettingsRuntimeOverridesTests
{
    [Fact]
    public void ApplySelectedPoliteness_WhenNoSelection_ReturnsSameInstance()
    {
        var settings = new VoiceDialogSettings { PolitenessLevel = PolitenessLevel.Formal };
        var userLanguageState = new UserLanguageState();

        var result = VoiceDialogSettingsRuntimeOverrides.ApplySelectedPoliteness(settings, userLanguageState);

        Assert.Same(settings, result);
    }

    [Fact]
    public void ApplySelectedPoliteness_WhenSelectionExists_OverridesPolitenessAndPreservesOtherSettings()
    {
        var settings = new VoiceDialogSettings
        {
            NoInputTimeoutMs = 1234,
            MaxRetries = 3,
            ConfirmationPolicy = ConfirmationPolicy.Always,
            SpeechRate = 1.25,
            PauseBetweenItemsMs = 456,
            CancelRefractoryMs = 789,
            AllowBargeIn = true,
            PolitenessLevel = PolitenessLevel.Formal,
            ProgressCalculationBasis = ProgressCalculationBasis.RequiredFieldsOnly
        };
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedPolitenessLevel(PolitenessLevel.Informal);

        var result = VoiceDialogSettingsRuntimeOverrides.ApplySelectedPoliteness(settings, userLanguageState);

        Assert.NotSame(settings, result);
        Assert.Equal(1234, result.NoInputTimeoutMs);
        Assert.Equal(3, result.MaxRetries);
        Assert.Equal(ConfirmationPolicy.Always, result.ConfirmationPolicy);
        Assert.Equal(1.25, result.SpeechRate);
        Assert.Equal(456, result.PauseBetweenItemsMs);
        Assert.Equal(789, result.CancelRefractoryMs);
        Assert.True(result.AllowBargeIn.HasValue && result.AllowBargeIn.Value);
        Assert.Equal(PolitenessLevel.Informal, result.PolitenessLevel);
        Assert.Equal(ProgressCalculationBasis.RequiredFieldsOnly, result.ProgressCalculationBasis);
    }

    [Fact]
    public void ApplyAllowBargeInOverride_WhenOverrideMissing_ReturnsSameInstance()
    {
        var settings = new VoiceDialogSettings { AllowBargeIn = false };

        var result = VoiceDialogSettingsRuntimeOverrides.ApplyAllowBargeInOverride(settings, allowBargeIn: null);

        Assert.Same(settings, result);
    }

    [Fact]
    public void ApplyAllowBargeInOverride_WhenOverrideExists_OverridesAndPreservesOtherSettings()
    {
        var settings = new VoiceDialogSettings
        {
            NoInputTimeoutMs = 1234,
            MaxRetries = 3,
            ConfirmationPolicy = ConfirmationPolicy.Always,
            SpeechRate = 1.25,
            PauseBetweenItemsMs = 456,
            CancelRefractoryMs = 789,
            AllowBargeIn = false,
            PolitenessLevel = PolitenessLevel.Formal,
            ProgressCalculationBasis = ProgressCalculationBasis.RequiredFieldsOnly
        };

        var result = VoiceDialogSettingsRuntimeOverrides.ApplyAllowBargeInOverride(settings, allowBargeIn: true);

        Assert.NotSame(settings, result);
        Assert.Equal(1234, result.NoInputTimeoutMs);
        Assert.Equal(3, result.MaxRetries);
        Assert.Equal(ConfirmationPolicy.Always, result.ConfirmationPolicy);
        Assert.Equal(1.25, result.SpeechRate);
        Assert.Equal(456, result.PauseBetweenItemsMs);
        Assert.Equal(789, result.CancelRefractoryMs);
        Assert.True(result.AllowBargeIn.HasValue && result.AllowBargeIn.Value);
        Assert.Equal(PolitenessLevel.Formal, result.PolitenessLevel);
        Assert.Equal(ProgressCalculationBasis.RequiredFieldsOnly, result.ProgressCalculationBasis);
    }

    [Fact]
    public void ApplyAutoAdvanceAfterCommitOverride_WhenOverrideMissing_ReturnsSameInstance()
    {
        var settings = new VoiceDialogSettings { AutoAdvanceAfterCommit = true };

        var result = VoiceDialogSettingsRuntimeOverrides.ApplyAutoAdvanceAfterCommitOverride(settings, autoAdvanceAfterCommit: null);

        Assert.Same(settings, result);
    }

    [Fact]
    public void ApplyAutoAdvanceAfterCommitOverride_WhenOverrideExists_OverridesAndPreservesOtherSettings()
    {
        var settings = new VoiceDialogSettings
        {
            NoInputTimeoutMs = 1234,
            MaxRetries = 3,
            ConfirmationPolicy = ConfirmationPolicy.Always,
            SpeechRate = 1.25,
            PauseBetweenItemsMs = 456,
            CancelRefractoryMs = 789,
            AllowBargeIn = true,
            AutoAdvanceAfterCommit = true,
            PolitenessLevel = PolitenessLevel.Formal,
            ProgressCalculationBasis = ProgressCalculationBasis.RequiredFieldsOnly
        };

        var result = VoiceDialogSettingsRuntimeOverrides.ApplyAutoAdvanceAfterCommitOverride(settings, autoAdvanceAfterCommit: false);

        Assert.NotSame(settings, result);
        Assert.Equal(1234, result.NoInputTimeoutMs);
        Assert.Equal(3, result.MaxRetries);
        Assert.Equal(ConfirmationPolicy.Always, result.ConfirmationPolicy);
        Assert.Equal(1.25, result.SpeechRate);
        Assert.Equal(456, result.PauseBetweenItemsMs);
        Assert.Equal(789, result.CancelRefractoryMs);
        Assert.True(result.AllowBargeIn.HasValue && result.AllowBargeIn.Value);
        Assert.False(result.AutoAdvanceAfterCommit.HasValue ? result.AutoAdvanceAfterCommit.Value : true);
        Assert.Equal(PolitenessLevel.Formal, result.PolitenessLevel);
        Assert.Equal(ProgressCalculationBasis.RequiredFieldsOnly, result.ProgressCalculationBasis);
    }

    [Fact]
    public void ApplyNoInputTimeoutOverride_WhenOverrideExists_PreservesSttConfidenceGates()
    {
        var settings = new VoiceDialogSettings
        {
            NoInputTimeoutMs = 10000,
            MaxRetries = 3,
            SttMinConfidenceToAccept = 91,
            SttMinConfidenceToConfirm = 73
        };

        var result = VoiceDialogSettingsRuntimeOverrides.ApplyNoInputTimeoutOverride(settings, noInputTimeoutMs: 18000);

        Assert.NotSame(settings, result);
        Assert.Equal(18000, result.NoInputTimeoutMs);
        Assert.Equal(91, result.SttMinConfidenceToAccept);
        Assert.Equal(73, result.SttMinConfidenceToConfirm);
    }

    [Fact]
    public void ApplyFormVoiceProfile_WhenAllowBargeInOverrideExists_OverridesAndPreservesOtherSettings()
    {
        var settings = new VoiceDialogSettings
        {
            NoInputTimeoutMs = 1234,
            MaxRetries = 3,
            ConfirmationPolicy = ConfirmationPolicy.Always,
            SpeechRate = 1.25,
            PauseBetweenItemsMs = 456,
            CancelRefractoryMs = 789,
            AllowBargeIn = false,
            PolitenessLevel = PolitenessLevel.Formal,
            ProgressCalculationBasis = ProgressCalculationBasis.RequiredFieldsOnly
        };

        var contract = new FormContract
        {
            Projections = new ContractProjections
            {
                Voice = new VoiceProjection
                {
                    FormVoiceProfile = new FormVoiceProfile
                    {
                        AllowBargeIn = true
                    }
                }
            }
        };

        var result = VoiceDialogSettingsRuntimeOverrides.ApplyFormVoiceProfile(settings, contract);

        Assert.NotSame(settings, result);
        Assert.Equal(1234, result.NoInputTimeoutMs);
        Assert.Equal(3, result.MaxRetries);
        Assert.Equal(ConfirmationPolicy.Always, result.ConfirmationPolicy);
        Assert.Equal(1.25, result.SpeechRate);
        Assert.Equal(456, result.PauseBetweenItemsMs);
        Assert.Equal(789, result.CancelRefractoryMs);
        Assert.True(result.AllowBargeIn.HasValue && result.AllowBargeIn.Value);
        Assert.Equal(PolitenessLevel.Formal, result.PolitenessLevel);
        Assert.Equal(ProgressCalculationBasis.RequiredFieldsOnly, result.ProgressCalculationBasis);
    }

    [Fact]
    public void ApplyFormVoiceProfile_WhenRuntimeOverridesExist_OverridesTimeoutRetryBudgetAndSpeechRate()
    {
        var settings = new VoiceDialogSettings
        {
            NoInputTimeoutMs = 10000,
            MaxRetries = 3,
            ConfirmationPolicy = ConfirmationPolicy.Always,
            SpeechRate = 1.0,
            PauseBetweenItemsMs = 456,
            CancelRefractoryMs = 789,
            AllowBargeIn = false,
            PolitenessLevel = PolitenessLevel.Formal,
            ProgressCalculationBasis = ProgressCalculationBasis.RequiredFieldsOnly
        };

        var contract = new FormContract
        {
            Projections = new ContractProjections
            {
                Voice = new VoiceProjection
                {
                    FormVoiceProfile = new FormVoiceProfile
                    {
                        NoInputTimeoutMs = 8000,
                        NoInputMaxRetries = 2,
                        SpeechRate = 1.3
                    }
                }
            }
        };

        var result = VoiceDialogSettingsRuntimeOverrides.ApplyFormVoiceProfile(settings, contract);

        Assert.NotSame(settings, result);
        Assert.Equal(8000, result.NoInputTimeoutMs);
        Assert.Equal(2, result.MaxRetries);
        Assert.Equal(ConfirmationPolicy.Always, result.ConfirmationPolicy);
        Assert.Equal(1.3, result.SpeechRate);
        Assert.Equal(456, result.PauseBetweenItemsMs);
        Assert.Equal(789, result.CancelRefractoryMs);
        Assert.False(result.AllowBargeIn.GetValueOrDefault());
        Assert.Equal(PolitenessLevel.Formal, result.PolitenessLevel);
        Assert.Equal(ProgressCalculationBasis.RequiredFieldsOnly, result.ProgressCalculationBasis);
    }
}
