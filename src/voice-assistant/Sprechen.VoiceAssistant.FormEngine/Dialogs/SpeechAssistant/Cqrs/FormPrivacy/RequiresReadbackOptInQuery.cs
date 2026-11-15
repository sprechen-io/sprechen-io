using Sprechen.Common.Cqrs;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormPrivacy;

/// <summary>
/// Fragt ab, ob das Rücklesen des Variablenwerts in einer Bestätigung eine ausdrückliche
/// Einwilligung erfordert.
/// </summary>
/// <param name="VariableId">Die Variablenkennung.</param>
internal sealed record RequiresReadbackOptInQuery(string VariableId) : IQuery<bool>;
