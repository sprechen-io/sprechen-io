using System.Reflection;
using System.Text.Json;
using Sprechen.Service.Deepgram.Services;
using Xunit;

namespace Sprechen.Service.Deepgram.Tests.Services;

/// <summary>
/// Vergleicht abgeleitete Erkennungsbegriffe je Sprache mit den direkt aus dem eingebetteten
/// Sprachbefehlskatalog gelesenen Äußerungen und prüft fehlende sowie zusätzliche Einträge.
/// </summary>
public sealed class CommandKeytermVocabularyDriftGuardTests
{
    private const string CanonicalResourceName =
        "Sprechen.Service.Deepgram.Tests.Resources.voice-commands.default.reference.json";

    [Fact]
    public void DerivedKeyterms_EqualCanonicalCommandUtterances()
    {
        var canonicalByLanguage = ReadCanonicalCommandUtterances();

        Assert.NotEmpty(canonicalByLanguage);
        foreach (var (language, canonical) in canonicalByLanguage)
        {
            var derived = CommandKeytermVocabulary.Default.GetKeyterms(language);

            Assert.NotEmpty(canonical);
            Assert.Empty(canonical.Except(derived, StringComparer.Ordinal));
            Assert.Empty(derived.Except(canonical, StringComparer.Ordinal));
        }
    }

    [Fact]
    public void DerivedKeyterms_ContainGermanSpellingModeTrigger()
    {
        var derived = CommandKeytermVocabulary.Default.GetKeyterms("de");

        Assert.Contains("buchstabieren", derived);
    }

    [Fact]
    public void DriftGuard_DetectsDivergence_WhenDerivedListMissesACanonicalUtterance()
    {
        var canonical = new[] { "weiter", "zuruck", "buchstabieren" };
        var derived = new CommandKeytermVocabulary(
            new Dictionary<string, IEnumerable<string>> { ["de"] = ["weiter", "zuruck"] }).GetKeyterms("de");

        Assert.NotEmpty(canonical.Except(derived, StringComparer.Ordinal));
    }

    [Fact]
    public void DriftGuard_DetectsDivergence_WhenDerivedListHasAnExtraEntry()
    {
        var canonical = new[] { "weiter", "zuruck" };
        var derived = new CommandKeytermVocabulary(
            new Dictionary<string, IEnumerable<string>> { ["de"] = ["weiter", "zuruck", "buchstabieren"] }).GetKeyterms("de");

        Assert.NotEmpty(derived.Except(canonical, StringComparer.Ordinal));
    }

    [Fact]
    public void GetKeyterms_WhenLanguageUnknownOrNull_IsEmpty()
    {
        Assert.Empty(CommandKeytermVocabulary.Default.GetKeyterms(null));
        Assert.Empty(CommandKeytermVocabulary.Default.GetKeyterms("zz"));
    }

    [Fact]
    public void Vocabulary_WhenResourceEmpty_YieldsEmptyListsWithoutThrowing()
    {
        var vocabulary = new CommandKeytermVocabulary(
            new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal));

        Assert.Empty(vocabulary.GetKeyterms("de"));
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> ReadCanonicalCommandUtterances()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(CanonicalResourceName)
            ?? throw new InvalidOperationException($"Canonical voice-command catalog resource '{CanonicalResourceName}' is not embedded.");
        using var document = JsonDocument.Parse(stream);
        var commands = document.RootElement.GetProperty("commands");

        var byLanguage = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var seenByLanguage = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var command in commands.EnumerateArray())
        {
            foreach (var utterance in command.GetProperty("utterances").EnumerateArray())
            {
                foreach (var localized in utterance.EnumerateObject())
                {
                    var text = localized.Value.GetString();
                    if (string.IsNullOrEmpty(text))
                    {
                        continue;
                    }

                    if (!byLanguage.TryGetValue(localized.Name, out var list))
                    {
                        list = [];
                        byLanguage[localized.Name] = list;
                        seenByLanguage[localized.Name] = new HashSet<string>(StringComparer.Ordinal);
                    }

                    if (seenByLanguage[localized.Name].Add(text))
                    {
                        list.Add(text);
                    }
                }
            }
        }

        return byLanguage.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<string>)pair.Value,
            StringComparer.Ordinal);
    }
}
