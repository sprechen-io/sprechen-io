namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet die deterministischen Trefferregeln, damit eine Auswahl ihre Auflösungsart mitführen
/// kann.
/// </summary>
public enum SelectionCandidateMatchKind
{
    None = 0,

    /// <summary>
    /// Kennzeichnet exakte Treffer normalisierter Anzeigenamen.
    /// </summary>
    Exact = 1,

    /// <summary>
    /// Kennzeichnet Treffer, bei denen der normalisierte Anzeigename im Suchtext
    /// enthalten ist oder umgekehrt.
    /// </summary>
    Partial = 2,

    /// <summary>
    /// Kennzeichnet Kennungstreffer, in denen das normalisierte Suchziel enthalten ist.
    /// </summary>
    Identifier = 3
}
