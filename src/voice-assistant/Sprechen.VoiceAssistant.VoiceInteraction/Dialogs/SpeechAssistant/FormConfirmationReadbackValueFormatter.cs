using System.Text;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Formatiert einen vom Aufrufer freigegebenen Bestätigungswert für die Sprachausgabe.
/// Erfassungsart, Buchstabierfreigabe und Zeichenform bestimmen Buchstabieren,
/// Ziffernausgabe oder Textausgabe. Gespeicherte Werte ändert dieser Formatierer nicht.
/// </summary>
internal sealed class FormConfirmationReadbackValueFormatter
{
    private readonly IPhoneticSpellOutRenderer _spellOutRenderer;

    public FormConfirmationReadbackValueFormatter(IPhoneticSpellOutRenderer spellOutRenderer)
    {
        _spellOutRenderer = spellOutRenderer ?? throw new ArgumentNullException(nameof(spellOutRenderer));
    }

    public string Format(
        string value,
        InputMode? inputMode,
        bool allowSpellOut,
        string isoCode,
        bool valueResolvedFromOption = false)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0 || !allowSpellOut)
        {
            // Ohne Buchstabierfreigabe zusammenhängende alphanumerische Werte mit
            // mindestens einer Ziffer und mehr als vier Zeichen in Viererblöcke gliedern.
            return FormatChunkedCodeRun(trimmed);
        }

        return inputMode switch
        {
            InputMode.Spelling => FormatSpelledValue(trimmed, isoCode),
            InputMode.Digits => string.Join(" ", trimmed.Select(character => character.ToString())),
            // Passende reine Großbuchstabenfolgen über das Buchstabieralphabet aufbereiten.
            // Aus Optionsnamen aufgelöste Werte von dieser Formheuristik ausnehmen.
            _ => !valueResolvedFromOption && IsSpokenLetterRun(trimmed)
                ? FormatSpelledValue(trimmed, isoCode)
                : trimmed
        };
    }

    // Codeförmige Werte in links beginnende Viererblöcke mit Leerzeichen dazwischen
    // gliedern. Andere Formen unverändert lassen. Betroffen ist nur der Ausgabetext.
    private static string FormatChunkedCodeRun(string trimmed)
    {
        if (!IsCodeRun(trimmed))
        {
            return trimmed;
        }

        var builder = new StringBuilder(trimmed.Length + (trimmed.Length / 4));
        for (var index = 0; index < trimmed.Length; index += 4)
        {
            if (index > 0)
            {
                builder.Append(' ');
            }

            builder.Append(trimmed, index, Math.Min(4, trimmed.Length - index));
        }

        return builder.ToString();
    }

    private static bool IsCodeRun(string value)
    {
        if (value.Length <= 4)
        {
            return false;
        }

        var hasDigit = false;
        foreach (var character in value)
        {
            if (char.IsDigit(character))
            {
                hasDigit = true;
                continue;
            }

            if (!char.IsLetter(character))
            {
                return false;
            }
        }

        return hasDigit;
    }

    // Die Zeichenform erkennen: mindestens zwei Buchstaben, keine Kleinbuchstaben
    // und außer Buchstaben nur Leerraum.
    private static bool IsSpokenLetterRun(string value)
    {
        var letters = 0;
        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character))
            {
                continue;
            }

            if (!char.IsUpper(character))
            {
                return false;
            }

            letters++;
        }

        return letters >= 2;
    }

    private string FormatSpelledValue(string trimmed, string isoCode)
        => _spellOutRenderer.Render(trimmed, isoCode);
}
