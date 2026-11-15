using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Services;

internal sealed class RecordingBackoffPolicy(int maxRetries) : IBackoffPolicy
{
    private readonly List<int> _requested = [];

    public int MaxRetries { get; } = maxRetries;

    public IReadOnlyList<int> RequestedAttemptIndices => _requested;

    public TimeSpan GetDelay(int attemptIndex)
    {
        _requested.Add(attemptIndex);
        return TimeSpan.Zero;
    }
}
