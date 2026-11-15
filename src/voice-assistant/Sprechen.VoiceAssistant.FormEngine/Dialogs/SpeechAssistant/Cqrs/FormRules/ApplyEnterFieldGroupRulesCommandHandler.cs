using System.Collections.Immutable;
using Sprechen.Common.Cqrs;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormRules;

/// <summary>
/// Führt die Regeln im Bereich <see cref="RuleScopes.OnEnterFieldGroup"/> über den
/// <see cref="FormRuleOrchestrator"/> aus und prüft danach den Fokus.
/// </summary>
internal sealed class ApplyEnterFieldGroupRulesCommandHandler : ICommandHandler<ApplyEnterFieldGroupRulesCommand, ImmutableArray<string>>
{
    private readonly FormRuleOrchestrator _ruleOrchestrator;
    private readonly FormNavigator _navigator;

    public ApplyEnterFieldGroupRulesCommandHandler(
        FormRuleOrchestrator ruleOrchestrator,
        FormNavigator navigator)
    {
        _ruleOrchestrator = ruleOrchestrator ?? throw new ArgumentNullException(nameof(ruleOrchestrator));
        _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
    }

    public Task<ImmutableArray<string>> HandleAsync(ApplyEnterFieldGroupRulesCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Bei Überschreiten der Iterationsgrenze stellt die Regelorchestrierung ihre
        // Momentaufnahme wieder her und ruft den Rückmeldungskanal auf. Danach wird hier
        // der Fokus geprüft.
        _ = _ruleOrchestrator.TryApplyRules(RuleScopes.OnEnterFieldGroup, out var promptIds);
        _navigator.EnsureFocusIsValid();
        return Task.FromResult(promptIds);
    }
}
