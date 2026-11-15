using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Zerlegt ein Mehrfachauswahlsegment ohne Gesamttreffer vollständig in benachbarte
/// Optionsbezeichnungen oder Codes, etwa „e mail sms“. Von links gewinnt jeweils die
/// längste passende Wortfolge. Bleibt ein Wort übrig, scheitert das gesamte Segment.
/// Bereits erkannte Teile werden dann nicht als Auswahl zurückgegeben.
/// </summary>
internal static class FormChoiceOptionSequenceDecomposer
{
    public static bool TryDecompose(
        string normalizedCandidate,
        ImmutableArray<FormChoiceOption> options,
        out ImmutableArray<string> codes)
    {
        codes = ImmutableArray<string>.Empty;
        var tokens = normalizedCandidate.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 2)
        {
            // Ein einzelnes Wort hat der Aufrufer bereits als exakten Gesamttreffer geprüft.
            return false;
        }

        var optionTokenForms = BuildOptionTokenForms(options);

        var matchedCodes = ImmutableArray.CreateBuilder<string>();
        var position = 0;
        while (position < tokens.Length)
        {
            if (!TryConsumeAt(tokens, position, optionTokenForms, out var code, out var consumedTokenCount))
            {
                return false;
            }

            matchedCodes.Add(code);
            position += consumedTokenCount;
        }

        codes = matchedCodes.ToImmutable();
        return true;
    }

    // Längste Bezeichnungen und Codes zuerst prüfen, damit etwa „fine settimana“ Vorrang
    // vor einer einteiligen Deutung von „fine“ erhält.
    private static (string[] Tokens, string Code)[] BuildOptionTokenForms(ImmutableArray<FormChoiceOption> options)
        => options
            .SelectMany(option => new[]
            {
                (Tokens: NormalizeToTokens(option.DisplayName), option.Code),
                (Tokens: NormalizeToTokens(option.Code), option.Code)
            })
            .Where(form => form.Tokens.Length > 0)
            .OrderByDescending(form => form.Tokens.Length)
            .ToArray();

    private static string[] NormalizeToTokens(string value)
        => TextNormalizer.NormalizeForPhraseMatching(value).Split(' ', StringSplitOptions.RemoveEmptyEntries);

    private static bool TryConsumeAt(
        string[] tokens,
        int position,
        (string[] Tokens, string Code)[] optionTokenForms,
        out string code,
        out int consumedTokenCount)
    {
        foreach (var (formTokens, formCode) in optionTokenForms)
        {
            if (MatchesAt(tokens, position, formTokens))
            {
                code = formCode;
                consumedTokenCount = formTokens.Length;
                return true;
            }
        }

        code = string.Empty;
        consumedTokenCount = 0;
        return false;
    }

    private static bool MatchesAt(string[] tokens, int position, string[] formTokens)
    {
        if (position + formTokens.Length > tokens.Length)
        {
            return false;
        }

        for (var i = 0; i < formTokens.Length; i++)
        {
            if (!string.Equals(tokens[position + i], formTokens[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
