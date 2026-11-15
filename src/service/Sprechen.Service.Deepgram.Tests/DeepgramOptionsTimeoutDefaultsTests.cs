using System;
using Sprechen.Service.Deepgram;
using Xunit;

namespace Sprechen.Service.Deepgram.Tests;

/// <summary>
/// Prüft die Standardwartezeiten von zwei Sekunden für Abschluss und Stopp der Live-Transkription.
/// </summary>
public sealed class DeepgramOptionsTimeoutDefaultsTests
{
    [Fact]
    public void LiveFinalizeWaitTimeout_DefaultsToTwoSeconds()
    {
        Assert.Equal(TimeSpan.FromSeconds(2), new DeepgramOptions().LiveFinalizeWaitTimeout);
    }

    [Fact]
    public void LiveStopTimeout_DefaultsToTwoSeconds()
    {
        Assert.Equal(TimeSpan.FromSeconds(2), new DeepgramOptions().LiveStopTimeout);
    }
}
