using System.Collections.Immutable;

namespace Sprechen.Common.Cqrs;

/// <summary>
/// Bietet denselben Ereignisspeicher ohne Tasks, damit synchrone Abläufe nicht auf
/// asynchrone APIs ausweichen müssen.
/// </summary>
public interface ISynchronousEventStore
{
    void Append(string aggregateId, IEnumerable<IDomainEvent> events);

    ImmutableArray<IDomainEvent> GetEvents(string aggregateId, long fromSequenceNumber = 0);

    long GetCurrentSequenceNumber(string aggregateId);
}


