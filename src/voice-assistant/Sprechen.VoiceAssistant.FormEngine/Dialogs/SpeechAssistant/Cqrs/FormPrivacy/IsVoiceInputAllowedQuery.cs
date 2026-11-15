using Sprechen.Common.Cqrs;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormPrivacy;

/// <summary>
/// Fragt ab, ob Spracheingabe für die angegebene Variable zulässig ist.
/// </summary>
/// <param name="VariableId">Die Variablenkennung.</param>
internal sealed record IsVoiceInputAllowedQuery(string VariableId) : IQuery<VoiceInputAllowedResult>;
