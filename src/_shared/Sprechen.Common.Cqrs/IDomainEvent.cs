namespace Sprechen.Common.Cqrs;

/// <summary>
/// Hält eine Zustandsänderung im Ereignisstrom eines Aggregats fest, damit sich der
/// Verlauf neu aufbauen lässt.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAt { get; }

    string AggregateId { get; }

    long SequenceNumber { get; }
}


