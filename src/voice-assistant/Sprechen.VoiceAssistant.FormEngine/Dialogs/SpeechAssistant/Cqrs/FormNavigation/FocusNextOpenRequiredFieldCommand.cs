using Sprechen.Common.Cqrs;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;

/// <summary>
/// Fordert die Navigation zum nächsten offenen Pflichtfeld aus der gemeinsamen Auswahl in
/// Dokumentreihenfolge an. Das Ergebnis unterscheidet unter anderem einen Umlauf und die
/// Rückmeldung, dass kein anderes Ziel fokussiert wurde.
/// </summary>
internal sealed record FocusNextOpenRequiredFieldCommand : ICommand<FocusNextOpenRequiredFieldResult>;
