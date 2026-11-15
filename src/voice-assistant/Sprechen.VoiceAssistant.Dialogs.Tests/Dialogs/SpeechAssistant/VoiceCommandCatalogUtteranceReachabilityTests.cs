using System.Text.RegularExpressions;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Localization;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die auflösbaren Formulierungen des eingebetteten Standard-Befehlskatalogs
/// mit dem echten Textparser für Sprache und Anredeform.
/// </summary>
public sealed class VoiceCommandCatalogUtteranceReachabilityTests
{
    private const string DefaultCatalog = "default";

    private const string PlaceholderArgument = "zielwert123";

    private static readonly string[] CatalogLabels = [DefaultCatalog];
    private static readonly PolitenessLevel[] PolitenessLevels = [PolitenessLevel.Formal, PolitenessLevel.Informal];

    // Bei diesen Befehlen folgt auf die Befehlsformulierung ein Argument.
    private static readonly string[] ArgumentAwareCommandIds =
    [
        GlobalCommandIds.JumpTo,
        GlobalCommandIds.SwitchForm,
        GlobalCommandIds.ChangeLanguage,
        GlobalCommandIds.ChangePoliteness
    ];

    // Diese Ausnahmen erlauben ae, oe oder ue als echte Buchstabenfolge.
    // Andere Vorkommen werden als mögliche ausgeschriebene Umlaute gemeldet.
    private static readonly string[] AllowedGermanDigraphTokens = ["aktuelles", "continue"];
    private static readonly Regex GermanDigraphPattern = new("(ae|oe|ue)", RegexOptions.Compiled);

    public static TheoryData<string, string, string, string, PolitenessLevel> CatalogUtteranceCases()
    {
        var data = new TheoryData<string, string, string, string, PolitenessLevel>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var catalogLabel in CatalogLabels)
        {
            foreach (var command in ResolveCommands(catalogLabel))
            {
                foreach (var catalogUtterance in command.Utterances)
                {
                    foreach (var isoCode in command.SupportedLanguages)
                    {
                        foreach (var politeness in PolitenessLevels)
                        {
                            if (!TryResolveUtterance(command, catalogUtterance, isoCode, politeness, out var text))
                            {
                                continue;
                            }

                            if (seen.Add($"{catalogLabel}|{command.CommandId}|{text}|{isoCode}|{politeness}"))
                            {
                                data.Add(catalogLabel, command.CommandId, text, isoCode, politeness);
                            }
                        }
                    }
                }
            }
        }

        return data;
    }

    public static TheoryData<string, string, string, string, PolitenessLevel> ArgumentAwareUtteranceCases()
    {
        var data = new TheoryData<string, string, string, string, PolitenessLevel>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var catalogLabel in CatalogLabels)
        {
            foreach (var command in ResolveCommands(catalogLabel))
            {
                if (!ArgumentAwareCommandIds.Contains(command.CommandId, StringComparer.Ordinal))
                {
                    continue;
                }

                foreach (var catalogUtterance in command.Utterances)
                {
                    foreach (var isoCode in command.SupportedLanguages)
                    {
                        foreach (var politeness in PolitenessLevels)
                        {
                            if (!TryResolveUtterance(command, catalogUtterance, isoCode, politeness, out var text))
                            {
                                continue;
                            }

                            if (seen.Add($"{catalogLabel}|{command.CommandId}|{text}|{isoCode}|{politeness}"))
                            {
                                data.Add(catalogLabel, command.CommandId, text, isoCode, politeness);
                            }
                        }
                    }
                }
            }
        }

        return data;
    }

    public static TheoryData<string, string, string, string> GermanUtteranceCases()
    {
        var data = new TheoryData<string, string, string, string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var catalogLabel in CatalogLabels)
        {
            foreach (var command in ResolveCommands(catalogLabel))
            {
                foreach (var catalogUtterance in command.Utterances)
                {
                    foreach (var entry in catalogUtterance)
                    {
                        if (!IsGermanKey(entry.Key))
                        {
                            continue;
                        }

                        if (seen.Add($"{catalogLabel}|{command.CommandId}|{entry.Key}|{entry.Value}"))
                        {
                            data.Add(catalogLabel, command.CommandId, entry.Key, entry.Value);
                        }
                    }
                }
            }
        }

        return data;
    }

    public static TheoryData<string, string, PolitenessLevel> CatalogLanguagePolitenessCases()
    {
        var data = new TheoryData<string, string, PolitenessLevel>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var catalogLabel in CatalogLabels)
        {
            foreach (var command in ResolveCommands(catalogLabel))
            {
                foreach (var isoCode in command.SupportedLanguages)
                {
                    foreach (var politeness in PolitenessLevels)
                    {
                        if (seen.Add($"{catalogLabel}|{isoCode}|{politeness}"))
                        {
                            data.Add(catalogLabel, isoCode, politeness);
                        }
                    }
                }
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(CatalogUtteranceCases))]
    public void EveryCatalogUtterance_ResolvesToItsOwningCommand(
        string catalogLabel,
        string expectedCommandId,
        string utteranceText,
        string isoCode,
        PolitenessLevel politeness)
    {
        var parsed = FormGlobalCommandParser.TryParse(
            CreateContract(catalogLabel),
            utteranceText,
            isoCode,
            politeness,
            out var commandId,
            out _);

        Assert.True(
            parsed,
            $"[{catalogLabel}] utterance '{utteranceText}' ({isoCode}/{politeness}) of '{expectedCommandId}' did not parse to any command.");
        Assert.Equal(expectedCommandId, commandId);
    }

    [Theory]
    [MemberData(nameof(ArgumentAwareUtteranceCases))]
    public void ArgumentAwareUtterances_WithArgument_ResolveToTheirOwningCommand(
        string catalogLabel,
        string expectedCommandId,
        string utteranceText,
        string isoCode,
        PolitenessLevel politeness)
    {
        var transcript = $"{utteranceText} {PlaceholderArgument}";

        var parsed = FormGlobalCommandParser.TryParse(
            CreateContract(catalogLabel),
            transcript,
            isoCode,
            politeness,
            out var commandId,
            out var argument);

        Assert.True(
            parsed,
            $"[{catalogLabel}] argument-extended utterance '{transcript}' ({isoCode}/{politeness}) of '{expectedCommandId}' did not parse to any command.");
        Assert.Equal(expectedCommandId, commandId);
        Assert.Equal(PlaceholderArgument, argument);
    }

    [Theory]
    [MemberData(nameof(GermanUtteranceCases))]
    public void GermanUtterances_ContainNoUnreachableTransliterationDigraphs(
        string catalogLabel,
        string commandId,
        string germanKey,
        string germanValue)
    {
        var offending = germanValue
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(token => GermanDigraphPattern.IsMatch(token.ToLowerInvariant()))
            .Where(token => !AllowedGermanDigraphTokens.Contains(token, StringComparer.Ordinal))
            .ToArray();

        Assert.True(
            offending.Length == 0,
            $"[{catalogLabel}] {commandId} {germanKey} '{germanValue}' contains unreachable transliteration digraph token(s): {string.Join(", ", offending)}");
    }

    [Theory]
    [MemberData(nameof(CatalogLanguagePolitenessCases))]
    public void NormalizedUtterances_AreUniquePerCatalogLanguageAndPoliteness(
        string catalogLabel,
        string isoCode,
        PolitenessLevel politeness)
    {
        var ownerByNormalizedUtterance = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var command in ResolveCommands(catalogLabel))
        {
            foreach (var catalogUtterance in command.Utterances)
            {
                if (!TryResolveUtterance(command, catalogUtterance, isoCode, politeness, out var text))
                {
                    continue;
                }

                var normalized = TextNormalizer.NormalizeForPhraseMatching(text);
                if (normalized.Length == 0)
                {
                    continue;
                }

                if (ownerByNormalizedUtterance.TryGetValue(normalized, out var owner)
                    && !string.Equals(owner, command.CommandId, StringComparison.Ordinal))
                {
                    Assert.Fail(
                        $"[{catalogLabel}] {isoCode}/{politeness} normalized utterance '{normalized}' is shared by commands '{owner}' and '{command.CommandId}'.");
                }

                ownerByNormalizedUtterance[normalized] = command.CommandId;
            }
        }
    }

    private static bool IsGermanKey(string key)
        => string.Equals(key, "de", StringComparison.Ordinal)
            || key.StartsWith("de@", StringComparison.Ordinal);

    private static bool TryResolveUtterance(
        ResolvedGlobalCommandDefinition command,
        IReadOnlyDictionary<string, string> catalogUtterance,
        string isoCode,
        PolitenessLevel politeness,
        out string text)
    {
        return LocalizedTextResolver.TryResolve(
                   catalogUtterance,
                   isoCode,
                   command.DefaultLanguage,
                   command.SupportedLanguages,
                   command.StrictLocalization,
                   politeness,
                   out text)
               && !string.IsNullOrWhiteSpace(text);
    }

    private static IReadOnlyList<ResolvedGlobalCommandDefinition> ResolveCommands(string catalogLabel)
    {
        _ = catalogLabel;
        return FormVoiceCommandCatalogResolver.ResolveDefaultCommands();
    }

    private static FormContract CreateContract(string catalogLabel)
    {
        _ = catalogLabel;
        return new FormContract
        {
            Projections = new ContractProjections
            {
                Voice = new VoiceProjection
                {
                    Hints = new VoiceHintsDefinition
                    {
                        CommandModelId = FormVoiceCommandModelIds.Default
                    }
                }
            }
        };
    }
}
