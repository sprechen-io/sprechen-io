using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormRules;

/// <summary>
/// Stellt die beiden Regelaufrufe für Gruppenwechsel und Absendeversuch bereit und gibt deren Ansagekennungen zurück.
/// </summary>
internal interface ICqrsFormRulesExecutor
{
    Task<ImmutableArray<string>> ApplySubmitAttemptRulesAsync();

    Task<ImmutableArray<string>> ApplyEnterFieldGroupRulesAsync();
}
