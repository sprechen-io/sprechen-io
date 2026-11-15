using Sprechen.Common.Cqrs;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormPrivacy;

/// <summary>
/// Fragt ab, ob für die Variable eine Möglichkeit zur stillen Eingabe angeboten werden soll.
/// </summary>
/// <param name="VariableId">Die Variablenkennung.</param>
internal sealed record ShouldOfferSilentInputOptionQuery(string VariableId) : IQuery<VoiceInputAllowedResult>;
