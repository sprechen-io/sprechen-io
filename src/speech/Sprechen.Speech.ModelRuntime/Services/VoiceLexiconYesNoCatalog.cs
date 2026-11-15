using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Speech.ModelRuntime.Services;

/// <summary>
/// Erschließt modellierte Ja-Nein-Wörter, Negationshinweise und Nichtverstehensausdrücke
/// je Sprache und verwendet bei fehlendem Sprachprofil das Modellstandardprofil.
/// </summary>
public sealed class VoiceLexiconYesNoCatalog
{
    private readonly VoiceInterface _model;
    private readonly Lazy<IReadOnlyDictionary<string, VoiceLexiconYesNoProfile>> _profiles;

    internal VoiceLexiconYesNoCatalog(VoiceInterface model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _profiles = new Lazy<IReadOnlyDictionary<string, VoiceLexiconYesNoProfile>>(BuildProfiles);
    }

    public string GetYesWord(string isoCode) => GetProfile(isoCode).YesWord;

    public string GetNoWord(string isoCode) => GetProfile(isoCode).NoWord;

    public IReadOnlyCollection<string> GetYesTokens(string isoCode) => GetProfile(isoCode).YesTokens;

    public IReadOnlyCollection<string> GetNoTokens(string isoCode) => GetProfile(isoCode).NoTokens;

    public IReadOnlyCollection<string> GetNegationCues(string isoCode) => GetProfile(isoCode).NegationCues;

    /// <summary>
    /// Wendungen, mit denen jemand anzeigt, dass er nicht verstanden hat — weder Ja noch Nein.
    /// </summary>
    public IReadOnlyCollection<string> GetNotUnderstoodPhrases(string isoCode)
        => GetProfile(isoCode).NotUnderstoodPhrases;


    private IReadOnlyDictionary<string, VoiceLexiconYesNoProfile> BuildProfiles()
    {
        return _model.YesNoProfiles.ToDictionary(
            profile => profile.LanguageCode,
            profile => profile,
            StringComparer.Ordinal);
    }

    private VoiceLexiconYesNoProfile GetProfile(string isoCode)
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

        throw new InvalidOperationException($"Voice surface model is missing a yes/no profile for '{normalizedIsoCode}'.");
    }
}
