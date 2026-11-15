using System.Collections.Immutable;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt lokalisierte Beschriftung, Hilfen, Beispiele und Auswahlmöglichkeiten für feldbezogene Ansagen bereit.
/// </summary>
public sealed record FormFieldPresentationSnapshot(
    string FieldGroupTitle,
    string Label,
    string? HelpText,
    ImmutableArray<string> Examples,
    ImmutableArray<FormChoiceOption> Options);
