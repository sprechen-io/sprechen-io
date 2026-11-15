using Sprechen.Common.Cqrs;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;

/// <summary>
/// Übernimmt Fokusbezüge und Zähler aus dem lesenden Formularzustand in das Ergebnis,
/// ohne selbst Zustandsänderungen anzufordern.
/// </summary>
internal sealed class GetFormStatusQueryHandler : IQueryHandler<GetFormStatusQuery, FormStatusResult>
{
    private readonly IReadableFormStatus _state;

    public GetFormStatusQueryHandler(IReadableFormStatus state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public Task<FormStatusResult> HandleAsync(GetFormStatusQuery query, CancellationToken cancellationToken = default)
    {
        var result = new FormStatusResult(
            CurrentFieldId: _state.CurrentFieldId,
            CurrentFieldGroupId: _state.CurrentFieldGroupId,
            CurrentFieldIndex: _state.CurrentFieldIndex,
            CurrentFieldGroupIndex: _state.CurrentFieldGroupIndex,
            TotalFields: _state.TotalFields,
            TotalFieldGroups: _state.TotalFieldGroups,
            OpenRequiredCount: _state.OpenRequiredCount,
            ValidationErrorCount: _state.ValidationErrorCount);

        return Task.FromResult(result);
    }
}


