namespace Sprechen.Service.Abstractions;

/// <summary>
/// Legt fest, ob Begriffe der Anfrage mit dem serverseitigen Vokabular des Erkennungsprofils kombiniert werden.
/// </summary>
public enum KeytermComposition
{
    /// <summary>
    /// Kombiniert die Begriffe der Anfrage mit dem serverseitigen Grundvokabular.
    /// </summary>
    WithServerBase = 0,

    /// <summary>
    /// Verwendet ausschließlich die Begriffe der Anfrage. Eine leere Liste schaltet auch das Grundvokabular aus.
    /// </summary>
    WithoutServerBase = 1
}
