using Sprechen.Speech.State;

namespace Sprechen.Speech.State.Tests.DialogExamples;

internal sealed class ExamplePromptEffect : IStateEffect<ExampleDialogEvent, ExampleDialogContext>
{
    private readonly string _prompt;

    public ExamplePromptEffect(string prompt)
    {
        _prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
    }

    public ValueTask ExecuteAsync(
        ExampleDialogContext context,
        IEventDispatcher<ExampleDialogEvent> dispatcher,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Prompts.Add(_prompt);
        return ValueTask.CompletedTask;
    }
}


