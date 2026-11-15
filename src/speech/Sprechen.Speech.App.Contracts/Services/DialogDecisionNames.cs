using System.Collections.Immutable;

namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Namen der unterstützten Dialogentscheidungen.
/// Die vollständige Liste steht für Prüfungen und Verbraucher bereit.
/// </summary>
public static class DialogDecisionNames
{
    public const string SpellingTerminator = "spelling.terminator";
    public const string SpellingBuffer = "spelling.buffer";
    public const string Confidence = "confidence";
    public const string ValueType = "value.type";

    public static ImmutableArray<string> All { get; } =
    [
        SpellingTerminator,
        SpellingBuffer,
        Confidence,
        ValueType
    ];
}
