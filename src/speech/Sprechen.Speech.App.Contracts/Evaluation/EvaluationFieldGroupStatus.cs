using System.Collections.Immutable;

namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Status einer Feldgruppe für die Auswertungstelemetrie: Relevanz,
/// Abschluss und Codes noch offener Gruppenfehler.
/// </summary>
public sealed record EvaluationFieldGroupStatus(
    string FieldGroupId,
    bool Relevant,
    bool Completed,
    ImmutableArray<string> OpenGroupErrorCodes);
