using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Führt passende Regeln bis zur Konvergenz oder zur Iterationsgrenze aus. Bei fehlender Konvergenz folgen Zustandswiederherstellung und konfigurierte Fehlerrückmeldung.
/// </summary>
internal sealed class FormRuleOrchestrator
{
    private readonly FormContract _contract;
    private readonly FormRuleEngine _ruleEngine;
    private readonly FormActionExecutor _actionExecutor;
    private readonly Func<FormDialogStateSnapshot> _takeSnapshot;
    private readonly Action<FormDialogStateSnapshot> _restoreSnapshot;
    private readonly IFormRuleLoopGuardFeedback _loopGuardFeedback;
    private readonly int _maxRuleIterations;

    public FormRuleOrchestrator(
        FormContract contract,
        FormRuleEngine ruleEngine,
        FormActionExecutor actionExecutor,
        Func<FormDialogStateSnapshot> takeSnapshot,
        Action<FormDialogStateSnapshot> restoreSnapshot,
        IFormRuleLoopGuardFeedback loopGuardFeedback,
        int maxRuleIterations)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(ruleEngine);
        ArgumentNullException.ThrowIfNull(actionExecutor);
        ArgumentNullException.ThrowIfNull(takeSnapshot);
        ArgumentNullException.ThrowIfNull(restoreSnapshot);
        ArgumentNullException.ThrowIfNull(loopGuardFeedback);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxRuleIterations, 1);

        _contract = contract;
        _ruleEngine = ruleEngine;
        _actionExecutor = actionExecutor;
        _takeSnapshot = takeSnapshot;
        _restoreSnapshot = restoreSnapshot;
        _loopGuardFeedback = loopGuardFeedback;
        _maxRuleIterations = maxRuleIterations;
    }

    public bool TryApplyRules(string scope, out ImmutableArray<string> promptIdsToSpeak)
    {
        promptIdsToSpeak = [];

        var applicableRules = GetApplicableRules(scope);
        if (applicableRules.Length == 0)
        {
            return true;
        }

        var snapshot = _takeSnapshot();
        var prompts = new List<string>();

        for (var iteration = 0; iteration < _maxRuleIterations; iteration++)
        {
            var changed = false;

            foreach (var rule in applicableRules)
            {
                if (rule.When.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var condition = _ruleEngine.EvaluateExpression(rule.When);
                var actions = condition ? rule.Then : rule.Else ?? [];

                foreach (var action in actions)
                {
                    changed |= _actionExecutor.ApplyAction(action, prompts);
                }
            }

            if (!changed)
            {
                promptIdsToSpeak = prompts.ToImmutableArray();
                return true;
            }
        }

        _restoreSnapshot(snapshot);
        // Nach fehlender Konvergenz ist der Zustand vor diesem Regelereignis wiederhergestellt.
        // Die konfigurierte Fehlerrückmeldung anfordern. Ob sie hörbar ausgegeben oder
        // protokolliert wird, hängt vom übergebenen Empfänger ab.
        _loopGuardFeedback.OnRuleLoopGuardBreach();
        return false;
    }

    private ImmutableArray<RuleDefinition> GetApplicableRules(string scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            return [];
        }

        return _contract.Rules
            .Where(rule =>
                !string.IsNullOrWhiteSpace(rule.Scope)
                && string.Equals(rule.Scope.Trim(), scope, StringComparison.OrdinalIgnoreCase))
            .OrderBy(rule => rule.Priority ?? 0)
            .ThenBy(rule => rule.RuleId, StringComparer.Ordinal)
            .ToImmutableArray();
    }
}


