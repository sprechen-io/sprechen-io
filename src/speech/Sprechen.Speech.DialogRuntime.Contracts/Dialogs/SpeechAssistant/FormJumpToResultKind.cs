namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ergebnisart eines gesprochenen Sprungkommandos über
/// <see cref="IFormDialogRuntimeNavigation.TryJumpToAsync"/>.
/// </summary>
public enum FormJumpToResultKind
{
    /// <summary>Das Ziel ist eingabefähig. Der Eingabefokus wurde dorthin verschoben.</summary>
    Focused,

    /// <summary>
    /// Ein schreibgeschütztes Feld oder eine ausschließlich schreibgeschützte Feldgruppe
    /// wird vorgelesen. Der Eingabefokus bleibt bestehen und es beginnt keine neue Aufnahme.
    /// </summary>
    SpokenReadOnlyTarget,

    /// <summary>Kein Feldlabel, kein Feldgruppentitel und kein Synonym traf das gesprochene Ziel.</summary>
    NotFound
}
