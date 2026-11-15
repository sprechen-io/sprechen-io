namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Benannte Fehlergründe beim Auflösen der Regeln für sensible Daten
/// durch <see cref="EffectiveSensitiveDataPolicyResolver.TryResolve"/>.
/// </summary>
public enum EffectiveSensitiveDataPolicyFailure
{
    None = 0,
    UnknownPrivacyClass,
    BlankVoiceInputPolicy,
    UnsupportedVoiceInputPolicy,
    BlankReadbackPolicy,
    UnsupportedReadbackPolicy,
    BlankSilentInputFallback,
    UnsupportedSilentInputFallback,
    SecretVoiceInputNotForbidden,
    SecretReadbackNotNever,
    SensitiveReadbackAlways
}
