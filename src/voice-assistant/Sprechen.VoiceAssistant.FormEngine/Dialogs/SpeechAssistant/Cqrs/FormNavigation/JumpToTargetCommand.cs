using Sprechen.Common.Cqrs;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;

/// <summary>
/// Fordert die Auflösung eines freien Sprungziels und den zugehörigen Feld- oder
/// Gruppenfokusversuch an. Aufgelöste Ziele ohne erfolgreichen Fokusversuch bleiben für
/// die Rückleseausgabe unterscheidbar.
/// </summary>
/// <param name="Target">Der Zielausdruck, etwa Feldbezeichnung, Gruppentitel oder Synonym.</param>
internal sealed record JumpToTargetCommand(FormJumpTarget Target) : ICommand<FormJumpExecution>;
