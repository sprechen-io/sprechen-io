namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Kennzeichnet den Interaktionskanal, damit die Evaluation die Studienbedingungen
/// Sprach- und HTML-Interaktion unterscheidet.
/// </summary>
public enum EvaluationChannel
{
    /// <summary>
    /// Sprachinteraktion über Erkennung und Sprachausgabe in Bedingung B.
    /// </summary>
    Voice,

    /// <summary>
    /// HTML-Interaktion mit einem Screenreader in Bedingung A.
    /// </summary>
    Html
}


