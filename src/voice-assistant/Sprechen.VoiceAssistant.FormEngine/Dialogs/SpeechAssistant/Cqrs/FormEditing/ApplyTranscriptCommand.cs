using Sprechen.Common.Cqrs;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormEditing;

/// <summary>
/// Fordert die Anwendung eines freien Transkripts auf das fokussierte Feld über dessen
/// werttypbezogenen Parsing- und Validierungsweg an.
/// </summary>
/// <param name="Transcript">Der unverarbeitete Transkripttext.</param>
internal sealed record ApplyTranscriptCommand(string Transcript) : ICommand<FormInputResult>;
