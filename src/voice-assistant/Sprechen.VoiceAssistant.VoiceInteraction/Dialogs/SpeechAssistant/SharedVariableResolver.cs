using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Löst gemeinsame Variablen zwischen Formularen anhand der vorgesehenen
/// Quellreihenfolge auf und hält übernehmbare Angaben und ihre Herkunft zusammen.
/// </summary>
internal sealed class SharedVariableResolver
{
    private readonly SharedVariableTargetVariableSet _targetVariableSet;
    private readonly SharedVariableSourceFormOrder _sourceFormOrder;
    private readonly SharedVariableContractUsageResolver _contractUsageResolver;
    private readonly SharedVariableAdoptionFlow _adoptionFlow;

    public SharedVariableResolver()
        : this(
            new SharedVariableTargetVariableSet(),
            new SharedVariableSourceFormOrder(),
            new SharedVariableContractUsageResolver(),
            new SharedVariableAdoptionFlow(
                new SharedVariableContractUsageResolver(),
                new SharedVariableAdoptionSourceResolver()))
    {
    }

    public SharedVariableResolver(
        SharedVariableTargetVariableSet targetVariableSet,
        SharedVariableSourceFormOrder sourceFormOrder,
        SharedVariableContractUsageResolver contractUsageResolver,
        SharedVariableAdoptionFlow adoptionFlow)
    {
        _targetVariableSet = targetVariableSet ?? throw new ArgumentNullException(nameof(targetVariableSet));
        _sourceFormOrder = sourceFormOrder ?? throw new ArgumentNullException(nameof(sourceFormOrder));
        _contractUsageResolver = contractUsageResolver ?? throw new ArgumentNullException(nameof(contractUsageResolver));
        _adoptionFlow = adoptionFlow ?? throw new ArgumentNullException(nameof(adoptionFlow));
    }

    public SharedVariableResolutionResult ApplySharedVariables(
        ApplicationDescriptor application,
        FormDescriptor targetForm,
        FormContract contract,
        SpeechAssistantApplicationSessionState applicationSession,
        ImmutableDictionary<string, string> initialValuesByVariableId)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(targetForm);
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(applicationSession);
        ArgumentNullException.ThrowIfNull(initialValuesByVariableId);

        var sharedVariables = _targetVariableSet.Resolve(targetForm);
        if (sharedVariables.Length == 0)
        {
            return new SharedVariableResolutionResult(initialValuesByVariableId, []);
        }

        if (applicationSession.FormsById.Count == 0)
        {
            return new SharedVariableResolutionResult(initialValuesByVariableId, []);
        }

        return _adoptionFlow.Apply(
            contract,
            applicationSession,
            initialValuesByVariableId,
            sharedVariables,
            _sourceFormOrder.Resolve(application, targetForm));
    }

    public bool IsVariableUsedInContract(FormContract contract, string variableId)
        => _contractUsageResolver.IsVariableUsedInContract(contract, variableId);
}

