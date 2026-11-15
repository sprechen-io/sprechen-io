using Sprechen.Common.Cqrs;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation.Events;

/// <summary>
/// Protokolliert das bisherige und das neue Fokusziel samt Grund für den Wechsel.
/// </summary>
public sealed record FocusMovedEvent : DomainEventBase
{
    public FieldId? PreviousFieldId { get; init; }
    public required FieldId NewFieldId { get; init; }
    public required FocusMovedReason Reason { get; init; }
}


