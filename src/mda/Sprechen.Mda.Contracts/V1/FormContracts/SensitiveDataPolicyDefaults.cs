namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Übertragungswerte für Regeln zum Umgang mit sensiblen Daten
/// (privacy-policy.schema.json). <see cref="EffectiveSensitiveDataPolicyResolver"/>
/// ergänzt Standardwerte und prüft zulässige Kombinationen.
/// </summary>
public static class SensitiveDataPolicyDefaults
{
    public const string VoiceInputForbidden = "forbidden";
    public const string VoiceInputOptional = "optional";
    public const string VoiceInputRecommended = "recommended";

    public const string ReadbackNever = "never";
    public const string ReadbackOptInRequired = "optInRequired";
    public const string ReadbackAlways = "always";

    public const string SilentInputFallbackKeyboard = "keyboard";
    public const string SilentInputFallbackBraille = "braille";
    public const string SilentInputFallbackAny = "any";
}
