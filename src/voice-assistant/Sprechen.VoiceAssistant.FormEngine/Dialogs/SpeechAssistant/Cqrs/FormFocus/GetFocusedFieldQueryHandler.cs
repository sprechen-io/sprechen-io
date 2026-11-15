using Sprechen.Common.Cqrs;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormFocus;

/// <summary>
/// Leitet die Fokusabfrage an den <see cref="FormFocusedFieldSnapshotBuilder"/> weiter,
/// damit dieselbe Feldprojektion auch über diesen Abfrageweg verwendet wird.
/// </summary>
internal sealed class GetFocusedFieldQueryHandler : IQueryHandler<GetFocusedFieldQuery, FormFieldSnapshot>
{
    private readonly FormFocusedFieldSnapshotBuilder _snapshotBuilder;

    public GetFocusedFieldQueryHandler(FormFocusedFieldSnapshotBuilder snapshotBuilder)
    {
        _snapshotBuilder = snapshotBuilder ?? throw new ArgumentNullException(nameof(snapshotBuilder));
    }

    public Task<FormFieldSnapshot> HandleAsync(GetFocusedFieldQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return Task.FromResult(_snapshotBuilder.Build());
    }
}
