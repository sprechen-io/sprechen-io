using System.Text.Json;
using Sprechen.Service.Abstractions;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Leitet sprachabhängige Erkennungsbegriffe aus den Äußerungen des eingebetteten Befehlskatalogs ab.
/// Leere Einträge und Duplikate werden entfernt.
/// </summary>
internal sealed class CommandKeytermVocabulary
{
    private const string CanonicalResourceName =
        "Sprechen.Service.Deepgram.Resources.voice-commands.default.reference.json";

    private const string CommandsPropertyName = "commands";
    private const string UtterancesPropertyName = "utterances";

    private static readonly IReadOnlyDictionary<string, IEnumerable<string>> EmptyMap =
        new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal);

    private static readonly CommandKeytermVocabulary DefaultInstance =
        new(ReadCanonicalKeyterms());

    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _keytermsByLanguage;

    internal CommandKeytermVocabulary(IReadOnlyDictionary<string, IEnumerable<string>> keytermsByLanguage)
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

    internal static CommandKeytermVocabulary Default => DefaultInstance;

    /// <summary>
    /// Liefert die Befehlsbegriffe der angefragten Sprache in Katalogreihenfolge.
    /// Fehlende oder unbekannte Sprachen ergeben eine leere Liste.
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

    private static IReadOnlyDictionary<string, IEnumerable<string>> ReadCanonicalKeyterms()
    {
        try
        {
            var assembly = typeof(CommandKeytermVocabulary).Assembly;
            using var stream = assembly.GetManifestResourceStream(CanonicalResourceName);
            if (stream is null)
            {
                return EmptyMap;
            }

            using var document = JsonDocument.Parse(stream);
            if (!document.RootElement.TryGetProperty(CommandsPropertyName, out var commands)
                || commands.ValueKind != JsonValueKind.Array)
            {
                return EmptyMap;
            }

            var byLanguage = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var command in commands.EnumerateArray())
            {
                if (!command.TryGetProperty(UtterancesPropertyName, out var utterances)
                    || utterances.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var utterance in utterances.EnumerateArray())
                {
                    if (utterance.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    foreach (var localized in utterance.EnumerateObject())
                    {
                        if (localized.Value.ValueKind != JsonValueKind.String)
                        {
                            continue;
                        }

                        var text = localized.Value.GetString();
                        if (string.IsNullOrEmpty(text))
                        {
                            continue;
                        }

                        if (!byLanguage.TryGetValue(localized.Name, out var list))
                        {
                            list = [];
                            byLanguage[localized.Name] = list;
                        }

                        list.Add(text);
                    }
                }
            }

            return byLanguage.ToDictionary(
                pair => pair.Key,
                pair => (IEnumerable<string>)pair.Value,
                StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return EmptyMap;
        }
    }
}
