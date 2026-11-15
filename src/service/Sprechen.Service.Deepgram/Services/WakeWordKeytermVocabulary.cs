using System.Text.Json;
using Sprechen.Service.Abstractions;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Liest Aktivierungswörter aus dem eingebetteten Wake-Word-Katalog.
/// Die Erkennungshinweise bestehen aus gemeinsamen Einleitungswörtern und dem Namen der gewählten Sprache.
/// </summary>
internal sealed class WakeWordKeytermVocabulary
{
    private const string CanonicalResourceName =
        "Sprechen.Service.Deepgram.Resources.wake-word-catalog.reference.json";

    private const string LeadInsPropertyName = "commandAnchorLeadIns";
    private const string LeadInPropertyName = "leadIn";
    private const string ProfilesPropertyName = "activationProfilesByLanguage";
    private const string LanguageCodePropertyName = "languageCode";
    private const string WakeWordNamePropertyName = "wakeWordName";

    private static readonly WakeWordKeytermVocabulary DefaultInstance = ReadCanonical();

    private readonly IReadOnlyList<string> _leadIns;
    private readonly IReadOnlyDictionary<string, string> _wakeWordNameByLanguage;

    internal WakeWordKeytermVocabulary(
        IReadOnlyList<string> leadIns,
        IReadOnlyDictionary<string, string> wakeWordNameByLanguage)
    {
        ArgumentNullException.ThrowIfNull(leadIns);
        ArgumentNullException.ThrowIfNull(wakeWordNameByLanguage);

        _leadIns = leadIns;

        var projected = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (language, name) in wakeWordNameByLanguage)
        {
            var iso = LanguageCodeNormalizer.NormalizeIso6391(language);
            if (iso is not null && !string.IsNullOrEmpty(name))
            {
                projected[iso] = name;
            }
        }

        _wakeWordNameByLanguage = projected;
    }

    internal static WakeWordKeytermVocabulary Default => DefaultInstance;

    /// <summary>
    /// Liefert eindeutige Einleitungswörter und den Wake-Word-Namen für die Sprache.
    /// Ohne gültigen Sprachcode oder passenden Namenseintrag bleibt die Liste leer.
    /// </summary>
    internal IReadOnlyList<string> GetKeyterms(string? languageCode)
    {
        var iso = LanguageCodeNormalizer.NormalizeIso6391(languageCode);
        if (iso is null || !_wakeWordNameByLanguage.TryGetValue(iso, out var wakeWordName))
        {
            return [];
        }

        var distinct = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var keyterm in _leadIns)
        {
            if (!string.IsNullOrEmpty(keyterm) && seen.Add(keyterm))
            {
                distinct.Add(keyterm);
            }
        }

        if (seen.Add(wakeWordName))
        {
            distinct.Add(wakeWordName);
        }

        return distinct;
    }

    private static WakeWordKeytermVocabulary ReadCanonical()
    {
        try
        {
            var assembly = typeof(WakeWordKeytermVocabulary).Assembly;
            using var stream = assembly.GetManifestResourceStream(CanonicalResourceName);
            if (stream is null)
            {
                return new WakeWordKeytermVocabulary([], new Dictionary<string, string>(StringComparer.Ordinal));
            }

            using var document = JsonDocument.Parse(stream);
            var root = document.RootElement;

            var leadIns = new List<string>();
            if (root.TryGetProperty(LeadInsPropertyName, out var leadInsElement)
                && leadInsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in leadInsElement.EnumerateArray())
                {
                    if (entry.ValueKind == JsonValueKind.Object
                        && entry.TryGetProperty(LeadInPropertyName, out var leadIn)
                        && leadIn.ValueKind == JsonValueKind.String)
                    {
                        var text = leadIn.GetString();
                        if (!string.IsNullOrEmpty(text))
                        {
                            leadIns.Add(text);
                        }
                    }
                }
            }

            var names = new Dictionary<string, string>(StringComparer.Ordinal);
            if (root.TryGetProperty(ProfilesPropertyName, out var profiles)
                && profiles.ValueKind == JsonValueKind.Array)
            {
                foreach (var profile in profiles.EnumerateArray())
                {
                    if (profile.ValueKind != JsonValueKind.Object
                        || !profile.TryGetProperty(LanguageCodePropertyName, out var lang)
                        || lang.ValueKind != JsonValueKind.String
                        || !profile.TryGetProperty(WakeWordNamePropertyName, out var name)
                        || name.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var languageCode = lang.GetString();
                    var wakeWordName = name.GetString();
                    if (!string.IsNullOrEmpty(languageCode) && !string.IsNullOrEmpty(wakeWordName))
                    {
                        names[languageCode] = wakeWordName;
                    }
                }
            }

            return new WakeWordKeytermVocabulary(leadIns, names);
        }
        catch (JsonException)
        {
            return new WakeWordKeytermVocabulary([], new Dictionary<string, string>(StringComparer.Ordinal));
        }
    }
}
