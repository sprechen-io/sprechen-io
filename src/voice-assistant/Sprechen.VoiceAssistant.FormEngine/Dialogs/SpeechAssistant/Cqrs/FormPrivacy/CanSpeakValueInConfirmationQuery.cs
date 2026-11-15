using Sprechen.Common.Cqrs;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormPrivacy;

/// <summary>
/// Fragt ab, ob der Variablenwert unter Berücksichtigung des übergebenen
/// Einwilligungskennzeichens in einer Bestätigungsansage gesprochen werden darf.
/// </summary>
/// <param name="VariableId">Die Variablenkennung.</param>
/// <param name="ExplicitOptIn">Die vom Aufrufer angegebene ausdrückliche Einwilligung.</param>
internal sealed record CanSpeakValueInConfirmationQuery(string VariableId, bool ExplicitOptIn) : IQuery<bool>;
