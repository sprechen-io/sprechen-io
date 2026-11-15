using System.Text.Json;
using Sprechen.Service.Abstractions;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Leitet sprachabhängige Bestätigungs- und Verneinungsbegriffe aus dem eingebetteten Sprachlexikon ab.
/// </summary>
internal sealed class ConfirmationKeytermVocabulary
{
    private const string CanonicalResourceName =
        "Sprechen.Service.Deepgram.Resources.voice-lexicon-catalog.reference.json";

    private const string ProfilesPropertyName = "yesNoProfiles";
    private const string LanguageCodePropertyName = "languageCode";
    private const string YesTokensPropertyName = "yesTokens";
    private const string NoTokensPropertyName = "noTokens";

    private static readonly ConfirmationKeytermVocabulary DefaultInstance = ReadCanonical();

    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _keytermsByLanguage;

    internal ConfirmationKeytermVocabulary(IReadOnlyDictionary<string, IEnumerable<string>> keytermsByLanguage)
    {
        ArgumentNullException.ThrowIfNull(keytermsByLanguage);

        var projected = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var (language, keyterms) in keytermsByLanguage)
        {
            var iso = LanguageCodeNormalizer.NormalizeIso6391(language);
            if (iso is null || keyterms is null)
            {
                continue;
            }

            var distinct = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var keyterm in keyterms)
            {
                if (!string.IsNullOrEmpty(keyterm) && seen.Add(keyterm))
                {
                    distinct.Add(keyterm);
                }
            }

            projected[iso] = distinct;
        }

        _keytermsByLanguage = projected;
    }

    internal static ConfirmationKeytermVocabulary Default => DefaultInstance;

    /// <summary>
    /// Liefert Bestätigungs- und Verneinungsbegriffe für die Anfragesprache. Ohne passende Sprache bleibt die Liste leer.
    /// </summary>
    internal IReadOnlyList<string> GetKeyterms(string? languageCode)
    {
        var iso = LanguageCodeNormalizer.NormalizeIso6391(languageCode);
        if (iso is not null && _keytermsByLanguage.TryGetValue(iso, out var keyterms))
        {
            return keyterms;
        }

        return [];
    }

    private static ConfirmationKeytermVocabulary ReadCanonical()
    {
        try
        {
            var assembly = typeof(ConfirmationKeytermVocabulary).Assembly;
            using var stream = assembly.GetManifestResourceStream(CanonicalResourceName);
            if (stream is null)
            {
                return new ConfirmationKeytermVocabulary(
                    new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal));
            }

            using var document = JsonDocument.Parse(stream);
            if (!document.RootElement.TryGetProperty(ProfilesPropertyName, out var profiles)
                || profiles.ValueKind != JsonValueKind.Array)
            {
                return new ConfirmationKeytermVocabulary(
                    new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal));
            }

            var byLanguage = new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal);
            foreach (var profile in profiles.EnumerateArray())
            {
                if (profile.ValueKind != JsonValueKind.Object
                    || !profile.TryGetProperty(LanguageCodePropertyName, out var lang)
                    || lang.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var languageCode = lang.GetString();
                if (string.IsNullOrEmpty(languageCode))
                {
                    continue;
                }

                var tokens = new List<string>();
                AppendTokens(profile, YesTokensPropertyName, tokens);
                AppendTokens(profile, NoTokensPropertyName, tokens);
                byLanguage[languageCode] = tokens;
            }

            return new ConfirmationKeytermVocabulary(byLanguage);
        }
        catch (JsonException)
        {
            return new ConfirmationKeytermVocabulary(
                new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal));
        }
    }

    private static void AppendTokens(JsonElement profile, string propertyName, List<string> tokens)
    {
        if (!profile.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var token in array.EnumerateArray())
        {
            if (token.ValueKind == JsonValueKind.String)
            {
                var text = token.GetString();
                if (!string.IsNullOrEmpty(text))
                {
                    tokens.Add(text);
                }
            }
        }
    }
}
