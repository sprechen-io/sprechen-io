using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.Speech.ModelRuntime.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Löst Datenschutzregeln für Werte und Vorlesewege auf. Freigabeprüfung und Maskentext bleiben unterscheidbare Operationen.
/// </summary>
internal sealed class FormPrivacyReadbackPolicy
{
    private readonly ImmutableDictionary<VariableId, EffectiveSensitiveDataPolicy> _effectivePoliciesByVariableId;

    public FormPrivacyReadbackPolicy(
        ImmutableDictionary<VariableId, EffectiveSensitiveDataPolicy> effectivePoliciesByVariableId)
    {
        _effectivePoliciesByVariableId = effectivePoliciesByVariableId
            ?? throw new ArgumentNullException(nameof(effectivePoliciesByVariableId));
    }

    public VoiceInputAllowedResult IsVoiceInputAllowed(string variableId)
    {
        var policy = RequirePolicy(variableId);
        return new VoiceInputAllowedResult(policy.AllowsVoiceInput, policy.SilentInputFallback);
    }

    public VoiceInputAllowedResult ShouldOfferSilentInputOption(string variableId)
    {
        var policy = RequirePolicy(variableId);
        return new VoiceInputAllowedResult(policy.OffersSilentInput, policy.SilentInputFallback);
    }

    public bool RequiresReadbackOptIn(string variableId)
        => RequirePolicy(variableId).RequiresReadbackOptIn;

    public bool CanSpeakValueInConfirmation(string variableId, bool explicitOptIn)
    {
        var policy = RequirePolicy(variableId);
        if (!policy.AllowsReadback)
        {
            return false;
        }

        return !policy.RequiresReadbackOptIn || explicitOptIn;
    }

    public bool IsSpellOutAllowedInConfirmation(string variableId)
        => RequirePolicy(variableId).AllowsSpelledReadback;

    public string BuildMaskedReadback(string variableId, string value, string isoCode)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        var policy = RequirePolicy(variableId);
        return SensitiveDataPolicies.BuildMaskedReadback(isoCode.Trim(), policy.PrivacyClass, value);
    }

    private EffectiveSensitiveDataPolicy RequirePolicy(string variableId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(variableId);

        if (!_effectivePoliciesByVariableId.TryGetValue(VariableId.From(variableId), out var policy))
        {
            throw new InvalidOperationException(
                "A resolved privacy policy was required for the requested variable and was not present.");
        }

        return policy;
    }
}
