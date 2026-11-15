using System.Collections.Immutable;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt die Zustände einzelner Felder und Feldgruppen zur Formularbeobachtung.
/// </summary>
public sealed record FormDetailedStatusSnapshot(
    ImmutableArray<FormFieldStatusSnapshot> Fields,
    ImmutableArray<FormFieldGroupStatusSnapshot> FieldGroups);


