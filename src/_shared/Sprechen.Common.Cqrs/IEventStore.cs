using System.Collections.Immutable;

namespace Sprechen.Common.Cqrs;

/// <summary>
/// Speichert und liest Ereignisse je Aggregat, damit dessen Verlauf anhand der
/// Ereignisfolge nachvollzogen werden kann.
/// </summary>
public interface IEventStore
{
    Task AppendAsync(string aggregateId, IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default);

    Task<ImmutableArray<IDomainEvent>> GetEventsAsync(string aggregateId, long fromSequenceNumber = 0, CancellationToken cancellationToken = default);

    Task<long> GetCurrentSequenceNumberAsync(string aggregateId, CancellationToken cancellationToken = default);
}


