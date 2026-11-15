using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Verwaltet Feldwerte und Zustandsabweichungen mit geprüften Kennungen und Änderungsrevision, damit spätere Zugriffe Zustandsänderungen erkennen können.
/// </summary>
internal sealed class FormMutableStateStore :
    IFormActionVisibilityState,
    IFormActionFieldAccessState,
    IFormActionVariableValueState
{
    private readonly Dictionary<string, string> _valuesByVariableId = new(StringComparer.Ordinal);
    private readonly Dictionary<FieldGroupId, bool> _fieldGroupVisibleById = new();
    private readonly Dictionary<FieldId, bool> _fieldVisibleById = new();
    private readonly Dictionary<FieldId, bool> _fieldEnabledById = new();
    private readonly Dictionary<FieldId, bool> _fieldReadOnlyById = new();
    private readonly Dictionary<FieldId, string> _fieldInputModeOverrides = new();
    private readonly IReadOnlySet<VariableId> _canonicalVariableIds;
    private readonly IReadOnlySet<FieldId> _canonicalFieldIds;
    private readonly IReadOnlySet<FieldGroupId> _canonicalFieldGroupIds;
    private long _stateRevision;

    internal FormMutableStateStore(
        IReadOnlySet<VariableId> canonicalVariableIds,
        IReadOnlySet<FieldId> canonicalFieldIds,
        IReadOnlySet<FieldGroupId> canonicalFieldGroupIds)
    {
        _canonicalVariableIds = canonicalVariableIds ?? throw new ArgumentNullException(nameof(canonicalVariableIds));
        _canonicalFieldIds = canonicalFieldIds ?? throw new ArgumentNullException(nameof(canonicalFieldIds));
        _canonicalFieldGroupIds = canonicalFieldGroupIds ?? throw new ArgumentNullException(nameof(canonicalFieldGroupIds));
    }

    public IReadOnlyDictionary<string, string> ValuesByVariableId => _valuesByVariableId;

    public IReadOnlyDictionary<FieldGroupId, bool> FieldGroupVisibleById => _fieldGroupVisibleById;

    public IReadOnlyDictionary<FieldId, bool> FieldVisibleById => _fieldVisibleById;

    public IReadOnlyDictionary<FieldId, bool> FieldEnabledById => _fieldEnabledById;

    public IReadOnlyDictionary<FieldId, bool> FieldReadOnlyById => _fieldReadOnlyById;

    public IReadOnlyDictionary<FieldId, string> FieldInputModeOverrides => _fieldInputModeOverrides;

    public FieldId? FocusedFieldId { get; private set; }

    /// <summary>
    /// Kennzeichnet den Änderungsstand des Speichers. Wirksame Einzeländerungen und jede
    /// Wiederherstellung erhöhen die Revision. Wörterbücher werden als IReadOnlyDictionary
    /// bereitgestellt. Die zugrunde liegenden Instanzen bleiben veränderlich.
    /// </summary>
    public long StateRevision => _stateRevision;

    internal ImmutableDictionary<string, string> GetValuesSnapshot()
        => _valuesByVariableId.ToImmutableDictionary(StringComparer.Ordinal);

    internal bool IsFieldVisible(FieldId fieldId)
        => _fieldVisibleById.GetValueOrDefault(fieldId, defaultValue: true);

    internal bool SetFieldVisible(FieldId fieldId, bool visible)
    {
        EnsureKnownField(fieldId);
        var previous = _fieldVisibleById.GetValueOrDefault(fieldId, defaultValue: true);
        _fieldVisibleById[fieldId] = visible;
        return RegisterEffectiveMutation(previous != visible);
    }

    internal bool IsFieldGroupVisible(FieldGroupId fieldGroupId)
        => _fieldGroupVisibleById.GetValueOrDefault(fieldGroupId, defaultValue: true);

    internal bool SetFieldGroupVisible(FieldGroupId fieldGroupId, bool visible)
    {
        EnsureKnownFieldGroup(fieldGroupId);
        var previous = _fieldGroupVisibleById.GetValueOrDefault(fieldGroupId, defaultValue: true);
        _fieldGroupVisibleById[fieldGroupId] = visible;
        return RegisterEffectiveMutation(previous != visible);
    }

    internal bool IsFieldEnabled(FieldId fieldId)
        => _fieldEnabledById.GetValueOrDefault(fieldId, defaultValue: true);

    internal bool SetFieldEnabled(FieldId fieldId, bool enabled)
    {
        EnsureKnownField(fieldId);
        var previous = _fieldEnabledById.GetValueOrDefault(fieldId, defaultValue: true);
        _fieldEnabledById[fieldId] = enabled;
        return RegisterEffectiveMutation(previous != enabled);
    }

    internal bool IsFieldReadOnly(FieldId fieldId)
        => _fieldReadOnlyById.GetValueOrDefault(fieldId, defaultValue: false);

    internal bool SetFieldReadOnly(FieldId fieldId, bool readOnly)
    {
        EnsureKnownField(fieldId);
        var previous = _fieldReadOnlyById.GetValueOrDefault(fieldId, defaultValue: false);
        _fieldReadOnlyById[fieldId] = readOnly;
        return RegisterEffectiveMutation(previous != readOnly);
    }

    internal bool SetFocusedField(FieldId? fieldId)
    {
        if (fieldId is { } focused)
        {
            EnsureKnownField(focused);
        }

        if (FocusedFieldId == fieldId)
        {
            return false;
        }

        FocusedFieldId = fieldId;
        return RegisterEffectiveMutation(mutationIsEffective: true);
    }

    internal bool SetFieldInputModeOverride(FieldId fieldId, string inputMode)
    {
        EnsureKnownField(fieldId);
        ArgumentException.ThrowIfNullOrWhiteSpace(inputMode);
        if (_fieldInputModeOverrides.TryGetValue(fieldId, out var existing) && existing == inputMode)
        {
            return false;
        }

        _fieldInputModeOverrides[fieldId] = inputMode;
        return RegisterEffectiveMutation(mutationIsEffective: true);
    }

    internal string? GetVariableValue(string variableId)
        => _valuesByVariableId.TryGetValue(variableId, out var value) ? value : null;

    internal bool SetVariableValue(string variableId, string value)
    {
        EnsureKnownVariable(variableId);
        var hasExisting = _valuesByVariableId.TryGetValue(variableId, out var existing);
        _valuesByVariableId[variableId] = value;
        return RegisterEffectiveMutation(!hasExisting || existing != value);
    }

    internal bool ClearVariableValue(string variableId)
    {
        EnsureKnownVariable(variableId);
        return RegisterEffectiveMutation(_valuesByVariableId.Remove(variableId));
    }

    /// <summary>
    /// Ersetzt den veränderlichen Zustand nach Prüfung seiner Kennungen durch die
    /// Momentaufnahme und erhöht die Revision auch bei gleichem Inhalt. Eine alte Revision
    /// wird dabei nicht aus der Momentaufnahme übernommen.
    /// </summary>
    internal void RestoreSnapshot(FormDialogStateSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ValidateRestore(snapshot);

        ReplaceContents(_valuesByVariableId, snapshot.ValuesByVariableId);
        ReplaceContents(_fieldGroupVisibleById, snapshot.FieldGroupVisibleById);
        ReplaceContents(_fieldVisibleById, snapshot.FieldVisibleById);
        ReplaceContents(_fieldEnabledById, snapshot.FieldEnabledById);
        ReplaceContents(_fieldReadOnlyById, snapshot.FieldReadOnlyById);
        ReplaceContents(_fieldInputModeOverrides, snapshot.FieldInputModeOverrides);
        FocusedFieldId = string.IsNullOrWhiteSpace(snapshot.FocusedFieldId)
            ? null
            : FieldId.From(snapshot.FocusedFieldId);
        _stateRevision++;
    }

    bool IFormActionVisibilityState.IsFieldVisible(FieldId fieldId)
        => IsFieldVisible(fieldId);

    bool IFormActionVisibilityState.SetFieldVisible(FieldId fieldId, bool visible)
        => SetFieldVisible(fieldId, visible);

    bool IFormActionVisibilityState.IsFieldGroupVisible(FieldGroupId fieldGroupId)
        => IsFieldGroupVisible(fieldGroupId);

    bool IFormActionVisibilityState.SetFieldGroupVisible(FieldGroupId fieldGroupId, bool visible)
        => SetFieldGroupVisible(fieldGroupId, visible);

    bool IFormActionFieldAccessState.IsFieldEnabled(FieldId fieldId)
        => IsFieldEnabled(fieldId);

    bool IFormActionFieldAccessState.SetFieldEnabled(FieldId fieldId, bool enabled)
        => SetFieldEnabled(fieldId, enabled);

    bool IFormActionFieldAccessState.IsFieldReadOnly(FieldId fieldId)
        => IsFieldReadOnly(fieldId);

    bool IFormActionFieldAccessState.SetFieldReadOnly(FieldId fieldId, bool readOnly)
        => SetFieldReadOnly(fieldId, readOnly);

    string? IFormActionVariableValueState.GetVariableValue(VariableId variableId)
        => GetVariableValue(variableId);

    bool IFormActionVariableValueState.SetVariableValue(VariableId variableId, string value)
        => SetVariableValue(variableId, value);

    bool IFormActionVariableValueState.ClearVariableValue(VariableId variableId)
        => ClearVariableValue(variableId);

    private void ValidateRestore(FormDialogStateSnapshot snapshot)
    {
        foreach (var variableId in snapshot.ValuesByVariableId.Keys)
        {
            EnsureKnownVariable(variableId);
        }

        foreach (var fieldId in snapshot.FieldVisibleById.Keys
            .Concat(snapshot.FieldEnabledById.Keys)
            .Concat(snapshot.FieldReadOnlyById.Keys)
            .Concat(snapshot.FieldInputModeOverrides.Keys))
        {
            EnsureKnownField(fieldId);
        }

        foreach (var fieldGroupId in snapshot.FieldGroupVisibleById.Keys)
        {
            EnsureKnownFieldGroup(fieldGroupId);
        }

        if (!string.IsNullOrWhiteSpace(snapshot.FocusedFieldId))
        {
            EnsureKnownField(FieldId.From(snapshot.FocusedFieldId));
        }
    }

    private void EnsureKnownVariable(string variableId)
    {
        if (!_canonicalVariableIds.Contains(VariableId.From(variableId)))
        {
            throw new InvalidOperationException("A variable identifier was not an exact canonical key.");
        }
    }

    private void EnsureKnownField(FieldId fieldId)
    {
        if (!_canonicalFieldIds.Contains(fieldId))
        {
            throw new InvalidOperationException("A field identifier was not an exact canonical key.");
        }
    }

    private void EnsureKnownFieldGroup(FieldGroupId fieldGroupId)
    {
        if (!_canonicalFieldGroupIds.Contains(fieldGroupId))
        {
            throw new InvalidOperationException("A field-group identifier was not an exact canonical key.");
        }
    }

    private bool RegisterEffectiveMutation(bool mutationIsEffective)
    {
        if (mutationIsEffective)
        {
            _stateRevision++;
        }

        return mutationIsEffective;
    }

    private static void ReplaceContents<TKey, TValue>(
        Dictionary<TKey, TValue> target,
        IReadOnlyDictionary<TKey, TValue> source)
        where TKey : notnull
    {
        target.Clear();
        foreach (var (key, value) in source)
        {
            target[key] = value;
        }
    }
}
