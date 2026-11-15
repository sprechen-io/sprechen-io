namespace Sprechen.Speech.State.Tests.DialogExamples;

internal sealed record ExampleDialogState
{
    public ExampleFieldId FocusField { get; init; }

    public ExampleDialogPhase Phase { get; init; } = ExampleDialogPhase.AwaitingInput;

    public bool IsSpellingMode { get; init; }

    public string SpellingBuffer { get; init; } = string.Empty;

    public ExampleAmbiguity? PendingAmbiguity { get; init; }

    public ExampleSuggestion? PendingSuggestion { get; init; }

    public IReadOnlyDictionary<ExampleFieldId, string> Values { get; init; } = new Dictionary<ExampleFieldId, string>();

    public static ExampleDialogState Create(ExampleFieldId focusField)
    {
        return new ExampleDialogState
        {
            FocusField = focusField
        };
    }

    public ExampleDialogState WithValue(ExampleFieldId field, string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var next = new Dictionary<ExampleFieldId, string>(Values)
        {
            [field] = value
        };

        return this with { Values = next };
    }

    public ExampleDialogState WithoutValue(ExampleFieldId field)
    {
        if (!Values.ContainsKey(field))
        {
            return this;
        }

        var next = new Dictionary<ExampleFieldId, string>(Values);
        next.Remove(field);

        return this with { Values = next };
    }
}


