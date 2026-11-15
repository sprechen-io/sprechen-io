using System.Collections.Immutable;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Bildet gespeicherte Optionscodes auf ihre bereits lokalisierten Anzeigenamen ab.
/// Mit <c>|</c> getrennte Mehrfachwerte werden als kommagetrennte Liste ausgegeben.
/// Fehlt eine passende Option, bleibt der jeweilige Code unverändert. Werte ohne Optionen
/// werden unverändert zurückgegeben.
/// </summary>
public static class FormChoiceOptionDisplayResolver
{
    public static string ResolveDisplayValue(ImmutableArray<FormChoiceOption> options, string value)
    {
        if (options.IsDefaultOrEmpty || string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (value.IndexOf('|', StringComparison.Ordinal) < 0)
        {
            return ResolveDisplayName(options, value);
        }

        var labels = value
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(code => ResolveDisplayName(options, code));
        return string.Join(", ", labels);
    }

    /// <summary>
    /// Prüft, ob jedes nichtleere Codesegment einer vorhandenen Option entspricht.
    /// Die Bestätigung kann solche Anzeigenamen direkt sprechen, statt beispielsweise
    /// das Optionslabel <c>SMS</c> wie einen frei diktierten Buchstabentext vorzulesen.
    /// </summary>
    public static bool AllCodesResolveToOptions(ImmutableArray<FormChoiceOption> options, string value)
    {
        if (options.IsDefaultOrEmpty || string.IsNullOrEmpty(value))
        {
            return false;
        }

        var codes = value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (codes.Length == 0)
        {
            return false;
        }

        foreach (var code in codes)
        {
            if (!options.Any(option => string.Equals(option.Code, code, StringComparison.Ordinal)))
            {
                return false;
            }
        }

        return true;
    }

    public static string ResolveDisplayName(ImmutableArray<FormChoiceOption> options, string code)
    {
        if (options.IsDefaultOrEmpty)
        {
            return code;
        }

        foreach (var option in options)
        {
            if (string.Equals(option.Code, code, StringComparison.Ordinal))
            {
                return option.DisplayName;
            }
        }

        return code;
    }
}
