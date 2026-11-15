using Sprechen.Common.Cqrs;
using Xunit;

namespace Sprechen.Common.Cqrs.Tests;

public sealed class InMemoryEventStoreTests
{
    #region Append Tests

    [Fact]
    public async Task AppendAsync_SingleEvent_StoresEvent()
    {
        var store = new InMemoryEventStore();
        var @event = new TestEvent { AggregateId = "session-1", SequenceNumber = 1 };

        await store.AppendAsync("session-1", [@event]);

        var events = await store.GetEventsAsync("session-1");
        Assert.Single(events);
        Assert.Equal("session-1", events[0].AggregateId);
    }

    [Fact]
    public async Task AppendAsync_MultipleEvents_StoresInOrder()
    {
        var store = new InMemoryEventStore();
        var events = new IDomainEvent[]
        {
            new TestEvent { AggregateId = "session-1", SequenceNumber = 1 },
            new TestEvent { AggregateId = "session-1", SequenceNumber = 2 },
            new TestEvent { AggregateId = "session-1", SequenceNumber = 3 }
        };

        await store.AppendAsync("session-1", events);

        var stored = await store.GetEventsAsync("session-1");
        Assert.Equal(3, stored.Length);
        Assert.Equal(1, stored[0].SequenceNumber);
        Assert.Equal(2, stored[1].SequenceNumber);
        Assert.Equal(3, stored[2].SequenceNumber);
    }

    [Fact]
    public async Task AppendAsync_DifferentAggregates_StoresSeparately()
    {
        var store = new InMemoryEventStore();
        var event1 = new TestEvent { AggregateId = "session-1", SequenceNumber = 1 };
        var event2 = new TestEvent { AggregateId = "session-2", SequenceNumber = 1 };

        await store.AppendAsync("session-1", [event1]);
        await store.AppendAsync("session-2", [event2]);

        var events1 = await store.GetEventsAsync("session-1");
        var events2 = await store.GetEventsAsync("session-2");
        Assert.Single(events1);
        Assert.Single(events2);
        Assert.Equal("session-1", events1[0].AggregateId);
        Assert.Equal("session-2", events2[0].AggregateId);
    }

    #endregion

    #region GetEvents Tests

    [Fact]
    public async Task GetEventsAsync_NoEvents_ReturnsEmpty()
    {
        var store = new InMemoryEventStore();

        var events = await store.GetEventsAsync("nonexistent");

        Assert.Empty(events);
    }

    [Fact]
    public async Task GetEventsAsync_FromSequenceNumber_ReturnsSubset()
    {
        var store = new InMemoryEventStore();
        var events = new IDomainEvent[]
        {
            new TestEvent { AggregateId = "session-1", SequenceNumber = 1 },
            new TestEvent { AggregateId = "session-1", SequenceNumber = 2 },
            new TestEvent { AggregateId = "session-1", SequenceNumber = 3 }
        };
        await store.AppendAsync("session-1", events);

        var result = await store.GetEventsAsync("session-1", fromSequenceNumber: 2);

        Assert.Equal(2, result.Length);
        Assert.Equal(2, result[0].SequenceNumber);
        Assert.Equal(3, result[1].SequenceNumber);
    }

    #endregion

    #region GetCurrentSequenceNumber Tests

    [Fact]
    public async Task GetCurrentSequenceNumberAsync_NoEvents_ReturnsZero()
    {
        var store = new InMemoryEventStore();

        var sequence = await store.GetCurrentSequenceNumberAsync("nonexistent");

        Assert.Equal(0, sequence);
    }

    [Fact]
    public async Task GetCurrentSequenceNumberAsync_WithEvents_ReturnsHighest()
    {
        var store = new InMemoryEventStore();
        var events = new IDomainEvent[]
        {
            new TestEvent { AggregateId = "session-1", SequenceNumber = 1 },
            new TestEvent { AggregateId = "session-1", SequenceNumber = 5 },
            new TestEvent { AggregateId = "session-1", SequenceNumber = 3 }
        };
        await store.AppendAsync("session-1", events);

        var sequence = await store.GetCurrentSequenceNumberAsync("session-1");

        Assert.Equal(5, sequence);
    }

    #endregion

}



