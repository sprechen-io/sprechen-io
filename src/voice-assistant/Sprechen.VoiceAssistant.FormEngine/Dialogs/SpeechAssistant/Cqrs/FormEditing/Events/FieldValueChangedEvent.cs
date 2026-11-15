using Sprechen.Common.Cqrs;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormEditing.Events;

/// <summary>
/// Hält den vorherigen und den übergebenen neuen Feldwert für das Ereignisprotokoll fest.
/// Die Ereignisdaten selbst verlangen keine Ungleichheit der beiden Werte.
/// </summary>
public sealed record FieldValueChangedEvent : DomainEventBase
{
    public required FieldId FieldId { get; init; }
    public required VariableId VariableId { get; init; }
    public string? PreviousValue { get; init; }
    public required string NewValue { get; init; }
}


