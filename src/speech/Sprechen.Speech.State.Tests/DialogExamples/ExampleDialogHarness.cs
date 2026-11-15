using Sprechen.Speech.State.Tests.TestDoubles;

namespace Sprechen.Speech.State.Tests.DialogExamples;

internal sealed class ExampleDialogHarness
{
    private readonly ExampleDialogReducer _reducer = new();
    private readonly ExampleDialogContext _context = new();

    public ExampleDialogState State { get; private set; }

    public ExampleDialogContext Context => _context;

    public ExampleDialogHarness(ExampleDialogState initialState)
    {
        State = initialState ?? throw new ArgumentNullException(nameof(initialState));
    }

    public async ValueTask DispatchAsync(ExampleDialogEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var update = _reducer.Reduce(State, @event);
        State = update.State;

        foreach (var effect in update.Effects)
        {
            await effect.ExecuteAsync(_context, NoOpEventDispatcher<ExampleDialogEvent>.Instance, CancellationToken.None);
        }
    }
}


