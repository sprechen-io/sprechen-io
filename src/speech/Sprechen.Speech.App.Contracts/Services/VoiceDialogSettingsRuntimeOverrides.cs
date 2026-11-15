using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Wendet Laufzeitpräferenzen und Formularvorgaben auf Dialogeinstellungen an, ohne die übergebenen Einstellungen zu verändern.
/// </summary>
public static class VoiceDialogSettingsRuntimeOverrides
{
    public static VoiceDialogSettings ApplySelectedPoliteness(
        VoiceDialogSettings settings,
        IUserLanguagePreferences userLanguageState)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(userLanguageState);

        var selectedPolitenessLevel = userLanguageState.SelectedPolitenessLevel;
        if (selectedPolitenessLevel is null || settings.PolitenessLevel == selectedPolitenessLevel)
        {
            return settings;
        }

        var overridden = Copy(settings);
        overridden.PolitenessLevel = selectedPolitenessLevel;

        return overridden;
    }

    public static VoiceDialogSettings ApplyAllowBargeInOverride(
        VoiceDialogSettings settings,
        bool? allowBargeIn)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (allowBargeIn is null || settings.AllowBargeIn == allowBargeIn.Value)
        {
            return settings;
        }

        var overridden = Copy(settings);
        overridden.AllowBargeIn = allowBargeIn.Value;

        return overridden;
    }

    public static VoiceDialogSettings ApplyAutoAdvanceAfterCommitOverride(
        VoiceDialogSettings settings,
        bool? autoAdvanceAfterCommit)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (autoAdvanceAfterCommit is null || settings.AutoAdvanceAfterCommit == autoAdvanceAfterCommit.Value)
        {
            return settings;
        }

        var overridden = Copy(settings);
        overridden.AutoAdvanceAfterCommit = autoAdvanceAfterCommit.Value;

        return overridden;
    }

    public static VoiceDialogSettings ApplyNoInputTimeoutOverride(
        VoiceDialogSettings settings,
        int? noInputTimeoutMs)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (noInputTimeoutMs is null || settings.NoInputTimeoutMs == noInputTimeoutMs.Value)
        {
            return settings;
        }

        var overridden = Copy(settings);
        overridden.NoInputTimeoutMs = noInputTimeoutMs.Value;

        return overridden;
    }

    public static VoiceDialogSettings ApplyMaxRetriesOverride(
        VoiceDialogSettings settings,
        int? maxRetries)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (maxRetries is null || settings.MaxRetries == maxRetries.Value)
        {
            return settings;
        }

        var overridden = Copy(settings);
        overridden.MaxRetries = maxRetries.Value;

        return overridden;
    }

    public static VoiceDialogSettings ApplySpeechRateOverride(
        VoiceDialogSettings settings,
        double? speechRate)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (speechRate is null || settings.SpeechRate == speechRate.Value)
        {
            return settings;
        }

        var overridden = Copy(settings);
        overridden.SpeechRate = speechRate.Value;

        return overridden;
    }

    public static VoiceDialogSettings ApplyFormVoiceProfile(
        VoiceDialogSettings settings,
        FormContract contract)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(contract);

        var profile = contract.Projections?.Voice?.FormVoiceProfile;
        if (profile is null)
        {
            return settings;
        }

        var overridden = ApplyAllowBargeInOverride(settings, profile.AllowBargeIn);
        overridden = ApplyNoInputTimeoutOverride(overridden, profile.NoInputTimeoutMs);
        overridden = ApplyMaxRetriesOverride(overridden, profile.NoInputMaxRetries);
        overridden = ApplySpeechRateOverride(overridden, profile.SpeechRate);

        return overridden;
    }

    private static VoiceDialogSettings Copy(VoiceDialogSettings settings)
    {
        return new VoiceDialogSettings
        {
            NoInputTimeoutMs = settings.NoInputTimeoutMs,
            MaxRetries = settings.MaxRetries,
            ConfirmationPolicy = settings.ConfirmationPolicy,
            SttMinConfidenceToAccept = settings.SttMinConfidenceToAccept,
            SttMinConfidenceToConfirm = settings.SttMinConfidenceToConfirm,
            SpeechRate = settings.SpeechRate,
            PauseBetweenItemsMs = settings.PauseBetweenItemsMs,
            CancelRefractoryMs = settings.CancelRefractoryMs,
            AllowBargeIn = settings.AllowBargeIn,
            AutoAdvanceAfterCommit = settings.AutoAdvanceAfterCommit,
            PolitenessLevel = settings.PolitenessLevel,
            ProgressCalculationBasis = settings.ProgressCalculationBasis
        };
    }
}
