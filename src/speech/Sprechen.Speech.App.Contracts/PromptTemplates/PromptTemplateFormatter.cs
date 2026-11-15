using System.Text.RegularExpressions;

namespace Sprechen.Speech.App.Contracts.PromptTemplates;

/// <summary>
/// Setzt Parameter in Ansagevorlagen ein und entfernt verbleibende Platzhalter
/// in doppelten geschweiften Klammern, damit sie nicht mitgesprochen werden.
/// </summary>
public static partial class PromptTemplateFormatter
{
    public static string Apply(
        string template,
        IReadOnlyDictionary<string, string?>? templateParameters)
    {
        if (string.IsNullOrEmpty(template))
        {
            return template;
        }

        var resolved = template;
        if (templateParameters is not null)
        {
            foreach (var parameter in templateParameters)
            {
                if (string.IsNullOrWhiteSpace(parameter.Key))
                {
                    continue;
                }

                var token = $"{{{{{parameter.Key.Trim()}}}}}";
                var value = parameter.Value?.Trim() ?? string.Empty;
                resolved = resolved.Replace(token, value, StringComparison.OrdinalIgnoreCase);
            }
        }

        return Tidy(resolved);
    }

    private static string Tidy(string text)
    {
        var stripped = UnresolvedTokenPattern().Replace(text, string.Empty);
        var collapsed = WhitespaceRunPattern().Replace(stripped, " ");
        return collapsed
            .Replace(" .", ".", StringComparison.Ordinal)
            .Replace(" ,", ",", StringComparison.Ordinal)
            .Trim();
    }

    [GeneratedRegex(@"\{\{[^}]*\}\}")]
    private static partial Regex UnresolvedTokenPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRunPattern();
}
