namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Verbindet die Keyterms einer Anfrage mit der Regel zum Einbezug des Serverwortschatzes.
/// Eine leere Liste mit <see cref="KeytermComposition.WithoutServerBase"/> verlangt keine
/// Erkennungshinweise. Fehlt dieser gesamte Wert, gilt der übliche Serverwortschatz.
/// </summary>
/// <param name="Terms">Modellierter statischer Wortschatz der Anfrage. Keine Transkripte oder eingegebenen Werte.</param>
/// <param name="Composition">Bestimmt, ob der Profilwortschatz des Servers hinzukommt.</param>
public sealed record SpeechKeyterms(
    IReadOnlyList<string> Terms,
    KeytermComposition Composition);
