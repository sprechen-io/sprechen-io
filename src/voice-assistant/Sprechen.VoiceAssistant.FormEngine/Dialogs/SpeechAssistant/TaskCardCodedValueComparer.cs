using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Vergleicht kodierte Soll- und Istwerte als Mengen. Anzeigetext der Vorgabe
/// wird auf Codes abgebildet. Unbekannte Ist-Token sind Klassenfehler.
/// </summary>
internal static class TaskCardCodedValueComparer
{
    internal static string Compare(string actual, string target, ImmutableArray<FormChoiceOption> options)
    {
        var codes = options.Select(option => option.Code).ToArray();
        var labels = options.Select(option => option.DisplayName).ToArray();
        var known = codes.Concat(labels).ToHashSet(StringComparer.Ordinal);
        var actualCodes = new List<string>();
        foreach (var token in Tokenize(actual, known))
        {
            var mapped = MapToken(token, codes, labels);
            if (mapped is null)
            {
                return TelemetryMismatchCodes.WrongFieldClass;
            }

            actualCodes.Add(mapped);
        }

        var targetCodes = new List<string>();
        foreach (var token in Tokenize(target, known))
        {
            var mapped = MapToken(token, codes, labels);
            if (mapped is not null)
            {
                targetCodes.Add(mapped);
            }
        }

        if (targetCodes.Count == 0)
        {
            return string.Equals(
                TaskCardComparisonKey.From(actual),
                TaskCardComparisonKey.From(target),
                StringComparison.Ordinal)
                ? TelemetryMismatchCodes.None
                : TelemetryMismatchCodes.TaskCardMismatch;
        }

        var actualSet = actualCodes.Distinct(StringComparer.Ordinal).OrderBy(code => code, StringComparer.Ordinal).ToArray();
        var targetSet = targetCodes.Distinct(StringComparer.Ordinal).OrderBy(code => code, StringComparer.Ordinal).ToArray();
        return actualSet.SequenceEqual(targetSet, StringComparer.Ordinal)
            ? TelemetryMismatchCodes.None
            : TelemetryMismatchCodes.TaskCardMismatch;
    }

    private static IEnumerable<string> Tokenize(string value, IReadOnlySet<string> knownTokens)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return [];
        }

        if (knownTokens.Contains(trimmed)
            || knownTokens.Any(token => string.Equals(token, trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            return [trimmed];
        }

        return trimmed.Split([',', '|'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static string? MapToken(string token, string[] codes, string[] labels)
    {
        var codeIndex = Array.IndexOf(codes, token);
        if (codeIndex >= 0)
        {
            return codes[codeIndex];
        }

        for (var index = 0; index < labels.Length; index++)
        {
            if (string.Equals(labels[index], token, StringComparison.OrdinalIgnoreCase))
            {
                return codes[index];
            }
        }

        return null;
    }
}
