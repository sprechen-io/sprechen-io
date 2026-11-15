using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Bestimmt Felder und Gruppen für Statusprojektionen. Ohne sichtbare Felder werden alle Felder verwendet.
/// Ohne beantwortbare Felder wird die ermittelte sichtbare Feldmenge übernommen.
/// </summary>
internal sealed class FormDialogProjectionScopeResolver
{
    private readonly ImmutableArray<string> _orderedFieldGroupIds;
    private readonly ImmutableArray<string> _orderedFieldIds;
    private readonly ImmutableDictionary<FieldId, string> _fieldToFieldGroupId;
    private readonly ImmutableHashSet<string> _answerableFieldIds;

    public FormDialogProjectionScopeResolver(
        ImmutableArray<string> orderedFieldGroupIds,
        ImmutableArray<string> orderedFieldIds,
        ImmutableDictionary<FieldId, string> fieldToFieldGroupId)
        : this(orderedFieldGroupIds, orderedFieldIds, fieldToFieldGroupId, orderedFieldIds.ToImmutableHashSet(StringComparer.Ordinal))
    {
    }

    public FormDialogProjectionScopeResolver(
        ImmutableArray<string> orderedFieldGroupIds,
        ImmutableArray<string> orderedFieldIds,
        ImmutableDictionary<FieldId, string> fieldToFieldGroupId,
        ImmutableHashSet<string> answerableFieldIds)
    {
        _orderedFieldGroupIds = orderedFieldGroupIds;
        _orderedFieldIds = orderedFieldIds;
        _fieldToFieldGroupId = fieldToFieldGroupId ?? throw new ArgumentNullException(nameof(fieldToFieldGroupId));
        _answerableFieldIds = answerableFieldIds ?? throw new ArgumentNullException(nameof(answerableFieldIds));
    }

    public FormDialogProjectionScope Resolve(
        Func<string, bool> isFieldEffectivelyVisible,
        string fallbackFieldGroupId)
    {
        ArgumentNullException.ThrowIfNull(isFieldEffectivelyVisible);

        var visibleFieldIds = BuildVisibleFieldIds(isFieldEffectivelyVisible);
        var relevantFieldGroupIds = BuildRelevantFieldGroupIds(visibleFieldIds, fallbackFieldGroupId);
        var answerableFieldIds = visibleFieldIds
            .Where(_answerableFieldIds.Contains)
            .ToImmutableArray();

        return new FormDialogProjectionScope(
            visibleFieldIds,
            relevantFieldGroupIds,
            answerableFieldIds.IsEmpty ? visibleFieldIds : answerableFieldIds);
    }

    private ImmutableArray<string> BuildVisibleFieldIds(Func<string, bool> isFieldEffectivelyVisible)
    {
        var visibleFieldIds = _orderedFieldIds
            .Where(isFieldEffectivelyVisible)
            .ToImmutableArray();

        return visibleFieldIds.Length == 0
            ? _orderedFieldIds
            : visibleFieldIds;
    }

    private ImmutableArray<string> BuildRelevantFieldGroupIds(
        ImmutableArray<string> visibleFieldIds,
        string fallbackFieldGroupId)
    {
        var relevantFieldGroupIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var fieldId in visibleFieldIds)
        {
            if (_fieldToFieldGroupId.TryGetValue(FieldId.From(fieldId), out var fieldGroupId))
            {
                _ = relevantFieldGroupIds.Add(fieldGroupId);
            }
        }

        var orderedRelevantFieldGroupIds = _orderedFieldGroupIds
            .Where(relevantFieldGroupIds.Contains)
            .ToList();

        if (!string.IsNullOrWhiteSpace(fallbackFieldGroupId)
            && !orderedRelevantFieldGroupIds.Contains(fallbackFieldGroupId, StringComparer.Ordinal))
        {
            orderedRelevantFieldGroupIds.Add(fallbackFieldGroupId);
        }

        return orderedRelevantFieldGroupIds.ToImmutableArray();
    }
}
