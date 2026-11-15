namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Liefert Zeichenzuordnungen zum Reduzieren oder Wiederherstellen deutscher Umlautumschreibungen bei der Textnormalisierung.
/// </summary>
internal static class GermanUmlautCharacterMap
{
    private static readonly IReadOnlyDictionary<char, char> CollapseMap = new Dictionary<char, char>
    {
        ['a'] = 'a',
        ['A'] = 'A',
        ['o'] = 'o',
        ['O'] = 'O',
        ['u'] = 'u',
        ['U'] = 'U'
    };

    private static readonly IReadOnlyDictionary<char, char> DiacriticsMap = new Dictionary<char, char>
    {
        ['a'] = '\u00E4',
        ['A'] = '\u00C4',
        ['o'] = '\u00F6',
        ['O'] = '\u00D6',
        ['u'] = '\u00FC',
        ['U'] = '\u00DC'
    };

    public static char Collapse(char value)
        => CollapseMap.TryGetValue(value, out var mappedValue) ? mappedValue : value;

    public static char RestoreDiacritics(char value)
        => DiacriticsMap.TryGetValue(value, out var mappedValue) ? mappedValue : value;
}
