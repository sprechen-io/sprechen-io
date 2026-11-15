using Sprechen.Common.Cqrs;
using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;

/// <summary>
/// Stellt Feldreihenfolge, Bezeichnungen und Werte als lesende Grundlage der
/// Formularzusammenfassung bereit.
/// </summary>
public interface IReadableFormSummary
{
    ImmutableArray<string> OrderedFieldIds { get; }

    string? GetFieldLabel(string fieldId);

    string? GetFieldValue(string fieldId);
}


