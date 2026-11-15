using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Sprechen.Common.Cqrs;

/// <summary>
/// Hält Ereignisströme im Prozess, damit Tests und die Clientseite den Verlauf ohne
/// Server speichern können.
/// </summary>
public sealed class InMemoryEventStore : IEventStore, ISynchronousEventStore
{
    private readonly ConcurrentDictionary<string, List<IDomainEvent>> _eventStreams = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    public Task AppendAsync(string aggregateId, IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        Append(aggregateId, events);
        return Task.CompletedTask;
    }

    public Task<ImmutableArray<IDomainEvent>> GetEventsAsync(string aggregateId, long fromSequenceNumber = 0, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetEvents(aggregateId, fromSequenceNumber));
    }

    public Task<long> GetCurrentSequenceNumberAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetCurrentSequenceNumber(aggregateId));
    }

    public void Append(string aggregateId, IEnumerable<IDomainEvent> events)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
        ArgumentNullException.ThrowIfNull(events);

        var eventList = events.ToList();
        if (eventList.Count == 0)
        {
            return;
        }

        lock (_lock)
        {
            var stream = _eventStreams.GetOrAdd(aggregateId, _ => []);
            stream.AddRange(eventList);
        }
    }

    public ImmutableArray<IDomainEvent> GetEvents(string aggregateId, long fromSequenceNumber = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);

        if (!_eventStreams.TryGetValue(aggregateId, out var stream))
        {
            return ImmutableArray<IDomainEvent>.Empty;
        }

        lock (_lock)
        {
            return stream
                .Where(e => e.SequenceNumber >= fromSequenceNumber)
                .OrderBy(e => e.SequenceNumber)
                .ToImmutableArray();
        }
    }

    public long GetCurrentSequenceNumber(string aggregateId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);

        if (!_eventStreams.TryGetValue(aggregateId, out var stream))
        {
            return 0L;
        }

        lock (_lock)
        {
            return stream.Count == 0 ? 0L : stream.Max(e => e.SequenceNumber);
        }
    }

}

