using Sprechen.Common.Cqrs;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormPrivacy;

/// <summary>
/// Fragt ab, ob der Variablenwert in einer Bestätigungsansage buchstabiert werden darf.
/// </summary>
/// <param name="VariableId">Die Variablenkennung.</param>
internal sealed record IsSpellOutAllowedInConfirmationQuery(string VariableId) : IQuery<bool>;
