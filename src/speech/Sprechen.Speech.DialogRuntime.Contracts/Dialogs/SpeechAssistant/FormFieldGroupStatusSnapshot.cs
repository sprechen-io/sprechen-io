using System.Collections.Immutable;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Überträgt Relevanz, Abschluss und offene Fehler einer Feldgruppe für die Formularbeobachtung.
/// </summary>
public sealed record FormFieldGroupStatusSnapshot(
    string FieldGroupId,
    bool Relevant,
    bool Completed,
    ImmutableArray<string> OpenGroupErrorCodes);


