namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Wörter aus dem Formular-Transkript-Lexikon, mit denen der Jahresrenderer
/// deutsche und englische Jahreszahlen ausschreibt. Ungenutzte Rollen bleiben leer.
/// </summary>
internal sealed class FormSpokenYearNumberGrammar
{
    internal static FormSpokenYearNumberGrammar Empty { get; } = new();

    // Deutsch: und verbindet Einer und Zehner, etwa in neunzehnhundertvierundachtzig.
    public string AndConnector { get; init; } = string.Empty;

    // Deutsch: ein in zusammengesetzten Zahlen wie einhundert und einundzwanzig,
    // im Unterschied zum alleinstehenden eins der Einertabelle.
    public string BoundOnePrefix { get; init; } = string.Empty;

    // Englisch: oh als gesprochenes Nullwort in Jahreszahlen wie nineteen oh five.
    public string OhWord { get; init; } = string.Empty;

    // Wort für Hundert: hundert auf Deutsch, hundred auf Englisch.
    public string HundredWord { get; init; } = string.Empty;

    // Wort für Tausend: tausend auf Deutsch, thousand auf Englisch.
    public string ThousandWord { get; init; } = string.Empty;
}
