using System.Text;

namespace Sprechen.Speech.TextNormalization;

/// <summary>
/// Zerlegt bereits normalisierten Text für wortbasierte Verbraucher. Die Wahl zwischen Buchstaben und zusätzlichen Ziffern bestimmt, was als Token erhalten bleibt.
/// </summary>
internal static class NormalizedTextTokenizer
{
    public static IEnumerable<string> TokenizeLetters(string normalizedText)
        => Tokenize(normalizedText, static ch => char.IsLetter(ch));

    public static IEnumerable<string> TokenizeLettersOrDigits(string normalizedText)
        => Tokenize(normalizedText, static ch => char.IsLetterOrDigit(ch));

    private static IEnumerable<string> Tokenize(string normalizedText, Func<char, bool> isTokenCharacter)
    {
        ArgumentNullException.ThrowIfNull(normalizedText);
        ArgumentNullException.ThrowIfNull(isTokenCharacter);

        var builder = new StringBuilder(normalizedText.Length);
        foreach (var ch in normalizedText)
        {
            if (isTokenCharacter(ch))
            {
                builder.Append(ch);
                continue;
            }

            if (builder.Length == 0)
            {
                continue;
            }

            yield return builder.ToString();
            builder.Clear();
        }

        if (builder.Length > 0)
        {
            yield return builder.ToString();
        }
    }
}
