namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Unveränderliche, aufgelöste Regeln für Spracheingabe und Rücklesen einer modellierten
/// Variablen, einschließlich der Datenschutzklasse und stiller Eingabemöglichkeiten.
/// </summary>
public sealed class EffectiveSensitiveDataPolicy
{
    internal EffectiveSensitiveDataPolicy(
        string privacyClass,
        string voiceInputPolicy,
        string readbackPolicy,
        string silentInputFallback)
    {
        PrivacyClass = privacyClass;
        VoiceInputPolicy = voiceInputPolicy;
        ReadbackPolicy = readbackPolicy;
        SilentInputFallback = silentInputFallback;
    }

    public string PrivacyClass { get; }

    public string VoiceInputPolicy { get; }

    public string ReadbackPolicy { get; }

    public string SilentInputFallback { get; }

    public bool AllowsVoiceInput
        => !string.Equals(VoiceInputPolicy, SensitiveDataPolicyDefaults.VoiceInputForbidden, StringComparison.Ordinal);

    public bool RequiresReadbackOptIn
        => string.Equals(ReadbackPolicy, SensitiveDataPolicyDefaults.ReadbackOptInRequired, StringComparison.Ordinal);

    public bool AllowsReadback
        => !string.Equals(ReadbackPolicy, SensitiveDataPolicyDefaults.ReadbackNever, StringComparison.Ordinal);

    public bool OffersSilentInput
        => PrivacyClass is PrivacyClasses.Sensitive or PrivacyClasses.Secret;

    public bool AllowsSpelledReadback
        => PrivacyClass is PrivacyClasses.Public or PrivacyClasses.Personal;
}
