using Sprechen.Common.Cqrs;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;

/// <summary>
/// Fordert die Navigation zu einer bestimmten Feldgruppe an.
/// </summary>
/// <param name="SessionId">Die Sitzungs- beziehungsweise Aggregatkennung des Ereignisstroms.</param>
/// <param name="FieldGroupId">Die Kennung der gewünschten Feldgruppe.</param>
internal sealed record NavigateToFieldGroupCommand(string SessionId, FieldGroupId FieldGroupId) : ICommand<NavigationResult>;


