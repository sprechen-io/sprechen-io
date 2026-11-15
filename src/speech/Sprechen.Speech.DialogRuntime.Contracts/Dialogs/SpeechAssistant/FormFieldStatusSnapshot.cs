using System.Collections.Immutable;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Überträgt Pflichtstatus, Relevanz und Abschluss eines Felds zur Beobachtung des Bearbeitungsstands einschließlich blockierender Fehler.
/// </summary>
public sealed record FormFieldStatusSnapshot(
    string FieldId,
    string FieldGroupId,
    bool Relevant,
    bool Required,
    bool Completed,
    ImmutableArray<string> BlockingErrorCodes);


