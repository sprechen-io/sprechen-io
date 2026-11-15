using System.Globalization;
using Sprechen.Speech.TextNormalization;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet normalisierte Monatsnamen und Genitivformen einer Kultur den Monaten
/// 1 bis 12 zu. Datumsparser verwenden damit dieselbe Normalisierung wie für
/// das Transkript und benötigen keine eigenen sprachbezogenen Monatstabellen.
/// </summary>
internal sealed class FormSpokenMonthNameIndex
{
    private readonly Dictionary<string, int> _monthsByName;

    public FormSpokenMonthNameIndex(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        _monthsByName = new Dictionary<string, int>(StringComparer.Ordinal);
        AddNames(culture.DateTimeFormat.MonthNames);
        AddNames(culture.DateTimeFormat.MonthGenitiveNames);
    }

    public bool TryGetMonth(string token, out int month)
        => _monthsByName.TryGetValue(token, out month);

    private void AddNames(IReadOnlyList<string> names)
    {
        // Nur die ersten zwölf Monatspositionen berücksichtigen und leere Namen auslassen.
        for (var i = 0; i < names.Count && i < 12; i++)
        {
            var normalized = TextNormalizer.NormalizeForPhraseMatching(names[i]);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                _monthsByName[normalized] = i + 1;
            }
        }
    }
}
