using System.Collections.Immutable;
using Sprechen.Common.Cqrs;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormRules;

/// <summary>
/// Führt die Regeln im Bereich <see cref="RuleScopes.OnSubmitAttempt"/> über den
/// <see cref="FormRuleOrchestrator"/> aus und prüft danach den Fokus.
/// </summary>
internal sealed class ApplySubmitAttemptRulesCommandHandler : ICommandHandler<ApplySubmitAttemptRulesCommand, ImmutableArray<string>>
{
    private readonly FormRuleOrchestrator _ruleOrchestrator;
    private readonly FormNavigator _navigator;

    public ApplySubmitAttemptRulesCommandHandler(
        FormRuleOrchestrator ruleOrchestrator,
        FormNavigator navigator)
    {
        _ruleOrchestrator = ruleOrchestrator ?? throw new ArgumentNullException(nameof(ruleOrchestrator));
        _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
    }

    public Task<ImmutableArray<string>> HandleAsync(ApplySubmitAttemptRulesCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Bei Überschreiten der Iterationsgrenze stellt die Regelorchestrierung ihre
        // Momentaufnahme wieder her und ruft den Rückmeldungskanal auf. Danach wird hier
        // der Fokus geprüft.
        _ = _ruleOrchestrator.TryApplyRules(RuleScopes.OnSubmitAttempt, out var promptIds);
        _navigator.EnsureFocusIsValid();
        return Task.FromResult(promptIds);
    }
}
