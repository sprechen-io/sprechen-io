using Sprechen.Common.Cqrs;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;

/// <summary>
/// Verbindet die Projektion der Formularübersicht mit der Momentaufnahme des
/// fokussierten Felds und der effektiven Feldsichtbarkeit.
/// </summary>
internal sealed class GetDiscoverySnapshotQueryHandler : IQueryHandler<GetDiscoverySnapshotQuery, FormDiscoverySnapshot>
{
    private readonly FormDiscoveryProjectionBuilder _discoveryProjectionBuilder;
    private readonly FormFocusedFieldSnapshotBuilder _focusedFieldSnapshotBuilder;
    private readonly FormFieldVisibilityPolicy _visibilityPolicy;

    public GetDiscoverySnapshotQueryHandler(
        FormDiscoveryProjectionBuilder discoveryProjectionBuilder,
        FormFocusedFieldSnapshotBuilder focusedFieldSnapshotBuilder,
        FormFieldVisibilityPolicy visibilityPolicy)
    {
        _discoveryProjectionBuilder = discoveryProjectionBuilder ?? throw new ArgumentNullException(nameof(discoveryProjectionBuilder));
        _focusedFieldSnapshotBuilder = focusedFieldSnapshotBuilder ?? throw new ArgumentNullException(nameof(focusedFieldSnapshotBuilder));
        _visibilityPolicy = visibilityPolicy ?? throw new ArgumentNullException(nameof(visibilityPolicy));
    }

    public Task<FormDiscoverySnapshot> HandleAsync(GetDiscoverySnapshotQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var focusedField = _focusedFieldSnapshotBuilder.Build();
        var snapshot = _discoveryProjectionBuilder.BuildDiscoverySnapshot(focusedField, _visibilityPolicy.IsFieldEffectivelyVisible);
        return Task.FromResult(snapshot);
    }
}
