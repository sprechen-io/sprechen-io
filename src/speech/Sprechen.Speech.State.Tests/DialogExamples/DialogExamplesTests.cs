namespace Sprechen.Speech.State.Tests.DialogExamples;

public sealed class DialogExamplesTests
{
    [Fact]
    public async Task LastName_SpellingOverride_CommitsNormalizedValue()
    {
        var harness = new ExampleDialogHarness(ExampleDialogState.Create(ExampleFieldId.LastName));

        await harness.DispatchAsync(new ExampleUserCommandEvent(ExampleDialogCommand.Change));
        await harness.DispatchAsync(new ExampleUserCommandEvent(ExampleDialogCommand.SpellingOn));

        foreach (var ch in "MÜLLER")
        {
            await harness.DispatchAsync(new ExampleUserSpelledCharEvent(ch));
        }

        await harness.DispatchAsync(new ExampleUserCommandEvent(ExampleDialogCommand.SpellingOff));

        Assert.Equal("Müller", harness.State.Values[ExampleFieldId.LastName]);
        Assert.Contains(harness.Context.Prompts, prompt => prompt.Contains("Übernommen", StringComparison.Ordinal));
    }

    [Fact]
    public async Task LastName_TwoOptionAmbiguity_RequiresExplicitChoice()
    {
        var harness = new ExampleDialogHarness(ExampleDialogState.Create(ExampleFieldId.LastName));

        await harness.DispatchAsync(new ExampleAmbiguityDetectedEvent(ExampleFieldId.LastName, "Müller", "Mueller"));

        Assert.Equal(ExampleDialogPhase.AwaitingAmbiguityChoice, harness.State.Phase);
        Assert.Contains(harness.Context.Prompts, prompt => prompt.Contains("Bitte eins oder zwei", StringComparison.Ordinal));

        await harness.DispatchAsync(new ExampleUserChoiceEvent(1));

        Assert.Equal("Müller", harness.State.Values[ExampleFieldId.LastName]);
        Assert.Equal(ExampleDialogPhase.AwaitingInput, harness.State.Phase);
    }

    [Fact]
    public async Task Street_MultiInformation_SuggestsPostalCityOptIn()
    {
        var harness = new ExampleDialogHarness(ExampleDialogState.Create(ExampleFieldId.Street));

        await harness.DispatchAsync(new ExampleUserTextEvent("Goethestraße 12, 80331 München."));

        Assert.Equal("Goethestraße 12", harness.State.Values[ExampleFieldId.Street]);
        Assert.Equal(ExampleDialogPhase.AwaitingSuggestionConfirmation, harness.State.Phase);
        Assert.NotNull(harness.State.PendingSuggestion);
        Assert.Equal("80331 München", harness.State.PendingSuggestion!.Value);

        await harness.DispatchAsync(new ExampleUserConfirmationEvent(true));

        Assert.Equal("80331 München", harness.State.Values[ExampleFieldId.PostalCity]);
        Assert.Equal(ExampleDialogPhase.AwaitingInput, harness.State.Phase);
    }

    [Fact]
    public async Task Email_InvalidFormat_RequestsSpelling()
    {
        var harness = new ExampleDialogHarness(ExampleDialogState.Create(ExampleFieldId.Email));

        await harness.DispatchAsync(new ExampleUserTextEvent("name(at)beispiel.de"));

        Assert.Equal(ExampleDialogPhase.AwaitingSpellingDecision, harness.State.Phase);
        Assert.Contains(harness.Context.Prompts, prompt => prompt.Contains("ungültiges Format", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FourDigitCode_BlockwiseInput_AllowsDeleteLastBlock()
    {
        var harness = new ExampleDialogHarness(ExampleDialogState.Create(ExampleFieldId.FourDigitCode));

        await harness.DispatchAsync(new ExampleUserTextEvent("Fünf sieben eins neun"));

        Assert.Equal("5719", harness.State.Values[ExampleFieldId.FourDigitCode]);
        Assert.Equal(ExampleDialogPhase.AwaitingCodeConfirmation, harness.State.Phase);

        await harness.DispatchAsync(new ExampleUserCommandEvent(ExampleDialogCommand.DeleteLastBlock));

        Assert.False(harness.State.Values.ContainsKey(ExampleFieldId.FourDigitCode));
        Assert.Contains(harness.Context.Prompts, prompt => prompt.Contains("Block gelöscht", StringComparison.Ordinal));
    }
}


