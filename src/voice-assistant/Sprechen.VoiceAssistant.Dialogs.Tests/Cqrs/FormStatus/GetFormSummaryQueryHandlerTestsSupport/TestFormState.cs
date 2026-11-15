using System.Collections.Immutable;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormStatus.GetFormSummaryQueryHandlerTestsSupport;

internal sealed class TestFormState : IReadableFormSummary
{
    private readonly Dictionary<string, (string Label, string? Value)> _fields;
    private readonly string[] _orderedFieldIds;

    public TestFormState(
        Dictionary<string, (string Label, string? Value)> fields,
        string[]? orderedFieldIds = null)
    {
        _fields = fields;
        _orderedFieldIds = orderedFieldIds ?? [.. fields.Keys];
    }

    public ImmutableArray<string> OrderedFieldIds => _orderedFieldIds.ToImmutableArray();

    public string? GetFieldLabel(string fieldId)
    {
        return _fields.TryGetValue(fieldId, out var field) ? field.Label : null;
    }

    public string? GetFieldValue(string fieldId)
    {
        return _fields.TryGetValue(fieldId, out var field) ? field.Value : null;
    }
}


