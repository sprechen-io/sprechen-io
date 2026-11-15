using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Speech.ModelRuntime.Services;

/// <summary>
/// Erschließt modellierte Aktivierungsformen, Detektorkatalog und Rückmeldungsregeln
/// für Aktivierungswörter sowie Vorgaben zur Mikrofonverfügbarkeit.
/// </summary>
public sealed class VoiceWakeWordModelCatalog
{
    private static readonly WakeWordActivationFormComposer Composer = new();
    private readonly VoiceInterface _model;
    private readonly Lazy<IReadOnlyDictionary<string, string[]>> _activationFormsByLanguage;
    private readonly Lazy<IReadOnlyDictionary<string, string[]>> _recognitionOnlyActivationFormsByLanguage;
    private readonly Lazy<string[]> _allActivationForms;

    internal VoiceWakeWordModelCatalog(VoiceInterface model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _activationFormsByLanguage = new Lazy<IReadOnlyDictionary<string, string[]>>(BuildActivationFormsByLanguage);
        _recognitionOnlyActivationFormsByLanguage = new Lazy<IReadOnlyDictionary<string, string[]>>(BuildRecognitionOnlyActivationFormsByLanguage);
        _allActivationForms = new Lazy<string[]>(BuildAllActivationForms);
    }

    public WakeWordDetectorCatalog DetectorCatalog => _model.WakeWordDetectorCatalog;

    public IReadOnlyList<string> GetActivationFormsForLanguage(string isoCode)
    {
        var normalizedIsoCode = NormalizeIsoCode(isoCode) ?? _model.DefaultLanguage;
        if (_activationFormsByLanguage.Value.TryGetValue(normalizedIsoCode, out var activationForms))
        {
            return activationForms;
        }

        if (_activationFormsByLanguage.Value.TryGetValue(_model.DefaultLanguage, out var defaultActivationForms))
        {
            return defaultActivationForms;
        }

        throw new InvalidOperationException($"Voice surface model is missing wake-word activation forms for '{normalizedIsoCode}'.");
    }

    public IReadOnlyList<string> GetRecognitionOnlyActivationFormsForLanguage(string isoCode)
    {
        var normalizedIsoCode = NormalizeIsoCode(isoCode) ?? _model.DefaultLanguage;
        if (_recognitionOnlyActivationFormsByLanguage.Value.TryGetValue(normalizedIsoCode, out var activationForms))
        {
            return activationForms;
        }

        if (_recognitionOnlyActivationFormsByLanguage.Value.TryGetValue(_model.DefaultLanguage, out var defaultActivationForms))
        {
            return defaultActivationForms;
        }

        return [];
    }

    public IReadOnlyList<string> GetAllActivationForms() => _allActivationForms.Value;

    public EarconId? GetRecoveryNotRecognizedEarconId()
    {
        var feedback = _model.WakeWordCatalog.RecoveryFeedback;
        if (feedback is null || string.IsNullOrWhiteSpace(feedback.NotRecognizedEarconId))
        {
            return null;
        }

        if (!Enum.TryParse<EarconId>(feedback.NotRecognizedEarconId, ignoreCase: false, out var parsed)
            || !Enum.IsDefined(parsed))
        {
            throw new InvalidOperationException(
                $"Wake-word recovery feedback earcon id '{feedback.NotRecognizedEarconId}' is not a known EarconId value.");
        }

        return parsed;
    }

    /// <summary>
    /// Liefert den modellierten Hinweiston für erfolgreiche Aktivierung oder null
    /// bei fehlender Angabe. Eine unbekannte Kennung löst einen Modellfehler aus,
    /// damit eine fehlerhafte Rückmeldung nicht unbemerkt zu Stille wird.
    /// </summary>
    public EarconId? GetActivationEarconId()
    {
        var feedback = _model.WakeWordCatalog.ActivationFeedback;
        if (feedback is null || string.IsNullOrWhiteSpace(feedback.ActivatedEarconId))
        {
            return null;
        }

        if (!Enum.TryParse<EarconId>(feedback.ActivatedEarconId, ignoreCase: false, out var parsed)
            || !Enum.IsDefined(parsed))
        {
            throw new InvalidOperationException(
                $"Wake-word activation feedback earcon id '{feedback.ActivatedEarconId}' is not a known EarconId value.");
        }

        return parsed;
    }

    public WakeUtteranceWindow? GetUtteranceWindow()
    {
        return _model.WakeWordCatalog.WakeUtteranceWindow;
    }

    public WakeRecoveryGuidanceSettings? GetRecoveryGuidanceSettings()
    {
        var feedback = _model.WakeWordCatalog.RecoveryFeedback;
        if (feedback?.GuidanceMissThreshold is not { } missThreshold
            || feedback.GuidanceCooldownSeconds is not { } cooldownSeconds
            || feedback.GuidanceMaxCooldownSeconds is not { } maxCooldownSeconds)
        {
            return null;
        }

        return new WakeRecoveryGuidanceSettings(missThreshold, cooldownSeconds, maxCooldownSeconds);
    }

    public MicrophoneAvailabilityPolicySettings GetMicrophoneAvailabilityPolicySettings()
    {
        var policy = _model.WakeWordCatalog.MicrophoneAvailabilityPolicy;
        return new MicrophoneAvailabilityPolicySettings(
            policy.FailureThreshold,
            policy.CooldownSeconds,
            policy.MaxCooldownSeconds,
            policy.AutomaticRetry,
            policy.AnnounceRecovery);
    }

    private IReadOnlyDictionary<string, string[]> BuildActivationFormsByLanguage()
    {
        var catalog = _model.WakeWordCatalog;
        return catalog.ActivationProfilesByLanguage.ToDictionary(
            profile => profile.LanguageCode,
            profile => NormalizeWakeWordValues(Composer.ComposeActivationForms(catalog, profile)),
            StringComparer.Ordinal);
    }

    private IReadOnlyDictionary<string, string[]> BuildRecognitionOnlyActivationFormsByLanguage()
    {
        var catalog = _model.WakeWordCatalog;
        return catalog.ActivationProfilesByLanguage.ToDictionary(
            profile => profile.LanguageCode,
            profile => NormalizeWakeWordValues(Composer.ComposeRecognitionOnlyActivationForms(catalog, profile)),
            StringComparer.Ordinal);
    }

    private string[] BuildAllActivationForms()
    {
        return _activationFormsByLanguage.Value
            .SelectMany(pair => pair.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private string[] NormalizeWakeWordValues(IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private string? NormalizeIsoCode(string isoCode)
    {
        if (string.IsNullOrWhiteSpace(isoCode))
        {
            return null;
        }

        return IsoCode.TryNormalizeIso6391(isoCode);
    }
}
