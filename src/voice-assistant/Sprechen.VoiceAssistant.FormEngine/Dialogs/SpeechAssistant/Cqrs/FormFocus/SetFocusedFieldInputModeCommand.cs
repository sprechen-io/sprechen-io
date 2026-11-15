using Sprechen.Common.Cqrs;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormFocus;

/// <summary>
/// Fordert einen abweichenden Eingabemodus für das aktuell fokussierte Feld an.
/// </summary>
/// <param name="InputMode">Der gewünschte Modus aus <see cref="Sprechen.Mda.Contracts.V1.FormContracts.InputModes"/>.</param>
internal sealed record SetFocusedFieldInputModeCommand(string InputMode) : ICommand<bool>;
