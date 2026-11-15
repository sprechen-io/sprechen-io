namespace Sprechen.Common.Cqrs;

/// <summary>
/// Gemeinsame Ereignisdaten mit Kennung, Zeitpunkt, Aggregatzuordnung und Sequenznummer.
/// Die Sequenznummer ermöglicht die Reihenfolge innerhalb eines Aggregats.
/// </summary>
public abstract record DomainEventBase : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public required string AggregateId { get; init; }
    public long SequenceNumber { get; init; }
}

