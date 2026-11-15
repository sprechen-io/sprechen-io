using Sprechen.Common.Cqrs;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;

/// <summary>
/// Fordert die Navigation zum nächsten verfügbaren Feld an.
/// </summary>
/// <param name="SessionId">Die Sitzungs- beziehungsweise Aggregatkennung des Ereignisstroms.</param>
internal sealed record NavigateNextCommand(string SessionId) : ICommand<NavigationResult>;


