using System;
using Sprechen.Service.Abstractions;
using Xunit;

namespace Sprechen.Service.Tests.Abstractions;

/// <summary>
/// Prüft Wertebereiche, Prozentumrechnung, Gleichheit und Verhalten nicht initialisierter Erkennungskonfidenzen.
/// </summary>
public sealed class RecognitionConfidenceTests
{
    [Fact]
    public void FromPercent_RoundTrips_Exactly_ForEveryPercent_0_to_100()
    {
        for (var percent = 0; percent <= 100; percent++)
        {
            Assert.Equal(percent, RecognitionConfidence.FromPercent(percent).AsPercent);
        }
    }

    [Theory]
    [InlineData(29)]
    [InlineData(57)]
    [InlineData(58)]
    public void FromPercent_RoundTrips_ForFloorSensitivePercents(int percent)
    {
        // Nach Division durch 100.0 und Rückmultiplikation können diese Werte knapp unter ihrer ursprünglichen Ganzzahl liegen.
        Assert.Equal(percent, RecognitionConfidence.FromPercent(percent).AsPercent);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void FromPercent_Throws_OutOfRange(int percent)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RecognitionConfidence.FromPercent(percent));
    }

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(1.0, 100)]
    [InlineData(0.85, 85)]
    [InlineData(0.849, 84)]   // Abrunden, nicht Runden.
    [InlineData(0.999, 99)]
    [InlineData(1.2, 100)]    // Endliche Werte außerhalb des Bereichs werden begrenzt.
    [InlineData(-0.3, 0)]
    public void FromNormalized_ReproducesFacadeFloorTimes100(double normalized, int expectedPercent)
    {
        Assert.Equal(expectedPercent, RecognitionConfidence.FromNormalized(normalized).AsPercent);
    }

    [Fact]
    public void FromNormalized_PreservesNormalizedValue()
    {
        Assert.Equal(0.849d, RecognitionConfidence.FromNormalized(0.849d).AsNormalized, 12);
        Assert.Equal(1.0d, RecognitionConfidence.FromNormalized(1.2d).AsNormalized, 12);
        Assert.Equal(0.0d, RecognitionConfidence.FromNormalized(-0.3d).AsNormalized, 12);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void FromNormalized_Throws_OnNonFinite(double value)
    {
        Assert.Throws<ArgumentException>(() => RecognitionConfidence.FromNormalized(value));
    }

    [Fact]
    public void Default_Access_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => default(RecognitionConfidence).AsNormalized);
        Assert.Throws<InvalidOperationException>(() => default(RecognitionConfidence).AsPercent);
    }

    [Fact]
    public void Default_ToString_DoesNotThrow()
    {
        // Auch nicht initialisierte Werte müssen etwa in einer Fehlerdiagnose ausgebbar sein.
        Assert.Equal("RecognitionConfidence(uninitialized)", default(RecognitionConfidence).ToString());
    }

    [Fact]
    public void Equality_IsByConstruction()
    {
        Assert.Equal(RecognitionConfidence.FromNormalized(0.85d), RecognitionConfidence.FromNormalized(0.85d));
        Assert.Equal(RecognitionConfidence.FromPercent(85), RecognitionConfidence.FromPercent(85));
        Assert.NotEqual(RecognitionConfidence.FromNormalized(0.85d), RecognitionConfidence.FromNormalized(0.86d));

        // FromPercent speichert hier 0.855, FromNormalized dagegen 0.85: gleicher Prozentwert,
        // aber unterschiedliche normalisierte Werte und damit keine Wertgleichheit.
        Assert.Equal(85, RecognitionConfidence.FromPercent(85).AsPercent);
        Assert.Equal(85, RecognitionConfidence.FromNormalized(0.85d).AsPercent);
        Assert.NotEqual(RecognitionConfidence.FromPercent(85), RecognitionConfidence.FromNormalized(0.85d));
    }
}
