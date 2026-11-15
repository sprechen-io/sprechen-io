using Sprechen.Common.Cqrs;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;

/// <summary>
/// Fordert den Fokus auf das nächste Feld mit einem Validierungsverstoß an und liefert
/// den zugehörigen Verstoß oder keinen Treffer, wenn kein solches Ziel fokussiert werden kann.
/// </summary>
internal sealed record FocusNextErrorCommand : ICommand<FormConstraintViolation?>;
