using Sprechen.Common.Cqrs;
using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;

/// <summary>
/// Liest Felder in der vorgegebenen Reihenfolge und übernimmt nur nichtleere Bezeichnungen
/// und Werte. Die Werte werden an den Rändern getrimmt. Eine eigene Sichtbarkeits- oder
/// Datenschutzfilterung führt dieser Handler nicht aus.
/// </summary>
internal sealed class GetFormSummaryQueryHandler : IQueryHandler<GetFormSummaryQuery, FormSummaryResult>
{
    private readonly IReadableFormSummary _state;

    public GetFormSummaryQueryHandler(IReadableFormSummary state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public Task<FormSummaryResult> HandleAsync(GetFormSummaryQuery query, CancellationToken cancellationToken = default)
    {
        var lines = new List<FormSummaryLine>();

        foreach (var fieldId in _state.OrderedFieldIds)
        {
            var label = _state.GetFieldLabel(fieldId);
            var value = _state.GetFieldValue(fieldId);

            if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            lines.Add(new FormSummaryLine(fieldId, label, value.Trim()));
        }

        return Task.FromResult(new FormSummaryResult(lines.ToImmutableArray()));
    }
}


