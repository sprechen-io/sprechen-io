using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormRules;

/// <summary>
/// Verbindet die Regelaufrufe bei Gruppenwechsel und Absendeversuch mit ihren Handlern, die anschließend den Fokus prüfen.
/// </summary>
internal sealed class CqrsFormRulesExecutor : ICqrsFormRulesExecutor
{
    private readonly ApplySubmitAttemptRulesCommandHandler _submitAttemptHandler;
    private readonly ApplyEnterFieldGroupRulesCommandHandler _enterFieldGroupHandler;

    public CqrsFormRulesExecutor(
        FormRuleOrchestrator ruleOrchestrator,
        FormNavigator navigator)
    {
        ArgumentNullException.ThrowIfNull(ruleOrchestrator);
        ArgumentNullException.ThrowIfNull(navigator);

        _submitAttemptHandler = new ApplySubmitAttemptRulesCommandHandler(ruleOrchestrator, navigator);
        _enterFieldGroupHandler = new ApplyEnterFieldGroupRulesCommandHandler(ruleOrchestrator, navigator);
    }

    public Task<ImmutableArray<string>> ApplySubmitAttemptRulesAsync()
        => _submitAttemptHandler.HandleAsync(new ApplySubmitAttemptRulesCommand());

    public Task<ImmutableArray<string>> ApplyEnterFieldGroupRulesAsync()
        => _enterFieldGroupHandler.HandleAsync(new ApplyEnterFieldGroupRulesCommand());
}
