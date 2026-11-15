using System.Collections.Immutable;

namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>Erzeugt strukturierte Diagnosen zu Buchstabierabschluss, Puffer, Konfidenz und Feldtypprüfung.</summary>
/// <remarks>
/// Die Felder sind für Kennungen, Zähler, Schwellen und Entscheidungscodes vorgesehen.
/// Aufrufer dürfen in die Stringparameter keine gesprochenen Texte oder Feldwerte eintragen.
/// </remarks>
public static class DialogDecisions
{
    /// <summary>
    /// Erfasst die Kennung des Abschlusskommandos sowie, ob es allein erkannt wurde.
    /// Die Tokenzahl hilft, einen isolierten Abschluss von einer eingebetteten Form zu unterscheiden.
    /// </summary>
    public static DialogDecision SpellingTerminator(string commandId, bool isolated, int transcriptTokenCount)
        => new(
            DialogDecisionNames.SpellingTerminator,
            [
                new DialogDecisionField("commandId", commandId),
                new DialogDecisionField("isolated", isolated ? "true" : "false"),
                new DialogDecisionField("transcriptTokens", transcriptTokenCount.ToString())
            ]);

    /// <summary>
    /// Erfasst Zeichen- und Blockzahl des Buchstabierpuffers beim Abschluss.
    /// So lässt sich ein leerer Puffer von einem erst später verworfenen Wert unterscheiden.
    /// </summary>
    public static DialogDecision SpellingBuffer(int characterCount, int blockCount)
        => new(
            DialogDecisionNames.SpellingBuffer,
            [
                new DialogDecisionField("characters", characterCount.ToString()),
                new DialogDecisionField("blocks", blockCount.ToString())
            ]);

    public static DialogDecision Confidence(int? confidencePercent, int thresholdPercent, string outcome)
        => new(
            DialogDecisionNames.Confidence,
            [
                new DialogDecisionField("value", confidencePercent is { } percent ? percent.ToString() : "unknown"),
                new DialogDecisionField("threshold", thresholdPercent.ToString()),
                new DialogDecisionField("outcome", outcome)
            ]);

    /// <summary>
    /// Erfasst Feldtyp, Wertlänge und Annahmeentscheidung mit ausdrücklichem Grund.
    /// So bleiben fehlendes Eingabeziel, leerer Wert und Typunverträglichkeit unterscheidbar.
    /// </summary>
    public static DialogDecision ValueType(
        string valueTypeKind,
        int valueLength,
        bool accepted,
        string notCapturedReason)
        => new(
            DialogDecisionNames.ValueType,
            [
                new DialogDecisionField("kind", valueTypeKind),
                new DialogDecisionField("length", valueLength.ToString()),
                new DialogDecisionField("accepted", accepted ? "true" : "false"),
                new DialogDecisionField("reason", notCapturedReason)
            ]);
}
