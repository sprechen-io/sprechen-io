using System.Collections.Immutable;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Überträgt Kategorie und mögliche Deutungen einer gescheiterten Transkriptinterpretation zur Auswahl passender Korrekturhinweise.
/// </summary>
public sealed record FormTranscriptParseFailure(
    FormTranscriptParseFailureCategory Category,
    string? Detail = null,
    string? AssumedInterpretation = null,
    ImmutableArray<string> Candidates = default)
{
    public ImmutableArray<string> Candidates { get; init; } = Candidates.IsDefault ? ImmutableArray<string>.Empty : Candidates;

    public static FormTranscriptParseFailure NotRecognized { get; } = new(FormTranscriptParseFailureCategory.NotRecognized);
}
