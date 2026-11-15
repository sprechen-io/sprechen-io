using System.Text;

namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Ersetzt deutsche ASCII-Umlautpaare mit einer Nachbarschaftsheuristik und einer übergebenen Zeichenzuordnung.
/// </summary>
internal static class GermanUmlautRewrite
{
    public static string Rewrite(string value, Func<char, char> mapBaseCharacter)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(mapBaseCharacter);

        if (value.Length < 2)
        {
            return value;
        }

        var builder = new StringBuilder(value.Length);
        for (var index = 0; index < value.Length; index++)
        {
            if (!ShouldRewrite(value, index))
            {
                builder.Append(value[index]);
                continue;
            }

            builder.Append(mapBaseCharacter(value[index]));
            index++;
        }

        return builder.ToString();
    }

    private static bool ShouldRewrite(string value, int index)
    {
        if (index < 0 || index + 1 >= value.Length)
        {
            return false;
        }

        var current = value[index];
        var next = value[index + 1];
        if (!IsSupportedBaseCharacter(current) || !IsSupportedE(next))
        {
            return false;
        }

        var previousCharacter = index > 0 ? value[index - 1] : (char?)null;
        var followingCharacter = index + 2 < value.Length ? value[index + 2] : (char?)null;
        return !IsPresentAsciiVowel(previousCharacter)
            && !IsPresentAsciiVowel(followingCharacter);
    }

    private static bool IsPresentAsciiVowel(char? value)
        => value.HasValue && IsAsciiVowel(value.Value);

    private static bool IsSupportedBaseCharacter(char value)
    {
        var normalized = char.ToLowerInvariant(value);
        return normalized is 'a' or 'o' or 'u';
    }

    private static bool IsSupportedE(char value)
        => value is 'e' or 'E';

    private static bool IsAsciiVowel(char value)
    {
        var normalized = char.ToLowerInvariant(value);
        return normalized is 'a' or 'e' or 'i' or 'o' or 'u' or 'y';
    }
}
