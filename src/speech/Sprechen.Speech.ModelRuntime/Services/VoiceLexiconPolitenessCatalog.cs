using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Speech.ModelRuntime.Services;

/// <summary>
/// Erschließt modellierte Höflichkeitswörter und Erkennungsformen je Sprache
/// und nutzt beim Profilabruf bei Bedarf das Profil der Standardsprache.
/// </summary>
public sealed class VoiceLexiconPolitenessCatalog
{
    private readonly VoiceInterface _model;
    private readonly Lazy<IReadOnlyDictionary<string, VoiceLexiconPolitenessProfile>> _profiles;

    internal VoiceLexiconPolitenessCatalog(VoiceInterface model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _profiles = new Lazy<IReadOnlyDictionary<string, VoiceLexiconPolitenessProfile>>(BuildProfiles);
    }

    public bool HasProfile(string isoCode)
    {
        var normalizedIsoCode = VoiceLexiconIsoCodeNormalizer.Normalize(isoCode);
        return normalizedIsoCode is not null && _profiles.Value.ContainsKey(normalizedIsoCode);
    }

    public string GetFormalWord(string isoCode) => GetProfile(isoCode).FormalWord;

    public string GetInformalWord(string isoCode) => GetProfile(isoCode).InformalWord;

    public IReadOnlyCollection<string> GetFormalTokens(string isoCode) => GetProfile(isoCode).FormalTokens;

    public IReadOnlyCollection<string> GetInformalTokens(string isoCode) => GetProfile(isoCode).InformalTokens;

    private IReadOnlyDictionary<string, VoiceLexiconPolitenessProfile> BuildProfiles()
    {
        return _model.PolitenessProfiles.ToDictionary(
            profile => profile.LanguageCode,
            profile => profile,
            StringComparer.Ordinal);
    }

    private VoiceLexiconPolitenessProfile GetProfile(string isoCode)
    {
        var normalizedIsoCode = VoiceLexiconIsoCodeNormalizer.Normalize(isoCode) ?? _model.DefaultLanguage;
        if (_profiles.Value.TryGetValue(normalizedIsoCode, out var profile))
        {
            return profile;
        }

        if (_profiles.Value.TryGetValue(_model.DefaultLanguage, out var defaultProfile))
        {
            return defaultProfile;
        }

        throw new InvalidOperationException($"Voice surface model is missing a politeness profile for '{normalizedIsoCode}'.");
    }
}
