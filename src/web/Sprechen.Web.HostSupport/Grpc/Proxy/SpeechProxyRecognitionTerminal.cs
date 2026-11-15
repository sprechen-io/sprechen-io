namespace Sprechen.Web.HostSupport.Grpc.Proxy;

/// <summary>
/// Unterscheidet Abschlusszustände einer Erkennung für das Proxyprotokoll.
/// Ein erfolgreich geliefertes leeres Transkript bleibt von einem fehlgeschlagenen Aufruf getrennt.
/// </summary>
internal enum SpeechProxyRecognitionTerminal
{

    Transcript,

    /// <summary>
    /// Das Backend schloss und lieferte keinen Text. Ein Domänenergebnis, kein Fehler.
    /// </summary>
    Empty,

    /// <summary>
    /// Eine Abbruchausnahme trat bei angefordertem Aufruferabbruch auf.
    /// </summary>
    CallerGone,

    /// <summary>
    /// Der Aufruf endete mit einem Fehler, der nicht als angeforderter Aufruferabbruch behandelt wurde.
    /// </summary>
    Failed,
}
