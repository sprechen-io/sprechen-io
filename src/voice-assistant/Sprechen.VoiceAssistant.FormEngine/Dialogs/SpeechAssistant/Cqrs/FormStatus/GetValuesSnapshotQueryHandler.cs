using System.Collections.Immutable;
using Sprechen.Common.Cqrs;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;

/// <summary>
/// Filtert die Momentaufnahme des <see cref="FormMutableStateStore"/> auf effektiv
/// sichtbare Variablen und bildet daraus ein unveränderliches Wörterbuch mit ordinalem
/// Kennungsvergleich.
/// </summary>
internal sealed class GetValuesSnapshotQueryHandler : IQueryHandler<GetValuesSnapshotQuery, ImmutableDictionary<string, string>>
{
    private readonly FormMutableStateStore _mutableState;
    private readonly FormFieldVisibilityPolicy _visibilityPolicy;

    public GetValuesSnapshotQueryHandler(
        FormMutableStateStore mutableState,
        FormFieldVisibilityPolicy visibilityPolicy)
    {
        _mutableState = mutableState ?? throw new ArgumentNullException(nameof(mutableState));
        _visibilityPolicy = visibilityPolicy ?? throw new ArgumentNullException(nameof(visibilityPolicy));
    }

    public Task<ImmutableDictionary<string, string>> HandleAsync(GetValuesSnapshotQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var visibleValues = _mutableState.GetValuesSnapshot()
            .Where(entry => _visibilityPolicy.IsVariableEffectivelyVisible(entry.Key))
            .ToImmutableDictionary(StringComparer.Ordinal);

        return Task.FromResult(visibleValues);
    }
}
