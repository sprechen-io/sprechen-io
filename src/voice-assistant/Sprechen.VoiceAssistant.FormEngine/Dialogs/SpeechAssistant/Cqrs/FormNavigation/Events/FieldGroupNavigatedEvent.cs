using Sprechen.Common.Cqrs;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation.Events;

/// <summary>
/// Protokolliert die bisherige und die neue Feldgruppe bei einer Navigation.
/// </summary>
public sealed record FieldGroupNavigatedEvent : DomainEventBase
{
    public FieldGroupId? PreviousFieldGroupId { get; init; }
    public required FieldGroupId NewFieldGroupId { get; init; }
}


