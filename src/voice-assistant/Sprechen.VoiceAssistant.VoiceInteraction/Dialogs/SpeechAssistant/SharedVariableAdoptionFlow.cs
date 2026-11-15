using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Ergänzt fehlende oder leere Anfangswerte mit aufgelösten gemeinsamen Angaben. Bereits vorhandene Werte bleiben erhalten. Validierung und Speicherung folgen außerhalb dieses Schritts.
/// </summary>
internal sealed class SharedVariableAdoptionFlow
{
    private readonly SharedVariableContractUsageResolver _contractUsageResolver;
    private readonly SharedVariableAdoptionSourceResolver _sourceResolver;

    public SharedVariableAdoptionFlow(
        SharedVariableContractUsageResolver contractUsageResolver,
        SharedVariableAdoptionSourceResolver sourceResolver)
    {
        _contractUsageResolver = contractUsageResolver ?? throw new ArgumentNullException(nameof(contractUsageResolver));
        _sourceResolver = sourceResolver ?? throw new ArgumentNullException(nameof(sourceResolver));
    }

    public SharedVariableResolutionResult Apply(
        FormContract contract,
        SpeechAssistantApplicationSessionState applicationSession,
        ImmutableDictionary<string, string> initialValuesByVariableId,
        ImmutableArray<string> sharedVariableIds,
        ImmutableArray<string> sourceFormIds)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(applicationSession);
        ArgumentNullException.ThrowIfNull(initialValuesByVariableId);

        var valuesByVariableId = initialValuesByVariableId;
        var adoptions = ImmutableArray.CreateBuilder<(string VariableId, string? SourceFormId)>();

        foreach (var variableId in sharedVariableIds)
        {
            if (!ShouldAdopt(contract, valuesByVariableId, variableId))
            {
                continue;
            }

            if (!_sourceResolver.TryResolve(applicationSession, sourceFormIds, variableId, out var candidate))
            {
                continue;
            }

            valuesByVariableId = valuesByVariableId.SetItem(candidate.VariableId, candidate.Value);
            adoptions.Add((candidate.VariableId, candidate.SourceFormId));
        }

        return new SharedVariableResolutionResult(valuesByVariableId, adoptions.ToImmutable());
    }

    private bool ShouldAdopt(
        FormContract contract,
        ImmutableDictionary<string, string> valuesByVariableId,
        string variableId)
        => _contractUsageResolver.IsVariableUsedInContract(contract, variableId)
            && (!valuesByVariableId.TryGetValue(variableId, out var existing) || string.IsNullOrWhiteSpace(existing));
}
