namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Heuristiken für deutsche ASCII-Umlautumschreibungen wie ae, oe und ue.
/// </summary>
public static class GermanUmlautTransliterator
{
    public static string CollapseAsciiTransliterations(string value)
        => GermanUmlautRewrite.Rewrite(value, GermanUmlautCharacterMap.Collapse);

    public static string RestorePreferredDiacritics(string value)
        => GermanUmlautRewrite.Rewrite(value, GermanUmlautCharacterMap.RestoreDiacritics);
}
