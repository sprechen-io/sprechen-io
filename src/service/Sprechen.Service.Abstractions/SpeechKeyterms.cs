namespace Sprechen.Service.Abstractions;

/// <summary>
/// Erkennungsbegriffe einer Anfrage mit dem Modus für die Kombination mit dem Servervokabular.
/// Eine leere Liste mit WithoutServerBase verhindert das Ergänzen des Servervokabulars.
/// </summary>
/// <param name="Terms">Begriffe, die vor der Anbieterübergabe durch SpeechKeytermPolicy zu prüfen sind.</param>
/// <param name="Composition">Modus für die Ergänzung des serverseitigen Profilvokabulars.</param>
public sealed record SpeechKeyterms(
    IReadOnlyList<string> Terms,
    KeytermComposition Composition);
