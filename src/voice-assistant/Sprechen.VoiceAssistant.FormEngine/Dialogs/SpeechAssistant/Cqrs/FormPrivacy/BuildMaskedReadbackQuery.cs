using Sprechen.Common.Cqrs;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormPrivacy;

/// <summary>
/// Fordert einen gemäß den Datenschutzregeln aufbereiteten Wert zum Vorlesen an. Die Regeln der Variablen bestimmen, ob der Wert maskiert wird.
/// </summary>
/// <param name="VariableId">Die Variablenkennung.</param>
/// <param name="Value">Der aufzubereitende Rohwert.</param>
/// <param name="IsoCode">Der Sprachcode für die Ausgabe, beispielsweise de oder en.</param>
internal sealed record BuildMaskedReadbackQuery(string VariableId, string Value, string IsoCode) : IQuery<string>;
