using Sprechen.Service.Abstractions;
using Xunit;

namespace Sprechen.Service.Tests.Abstractions;

/// <summary>
/// Prüft Prozentumrechnung, Bereichsbegrenzung und fehlende oder nicht endliche Konfidenzwerte.
/// </summary>
public sealed class SttConfidencePercentVoParityTests
{
    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(1.0, 100)]
    [InlineData(0.85, 85)]
    [InlineData(0.849, 84)]
    [InlineData(0.999, 99)]
    [InlineData(0.005, 0)]
    [InlineData(0.295, 29)]   // Mittelpunkt des Prozentintervalls für 29.
    [InlineData(1.2, 100)]    // Endliche Werte außerhalb des Bereichs werden begrenzt.
    [InlineData(-0.3, 0)]
    public void FromNormalized_Double_ByteIdenticalForFinite(double normalized, int expectedPercent)
    {
        Assert.Equal(expectedPercent, SttConfidencePercent.FromNormalized(normalized));
    }

    [Fact]
    public void FromNormalized_Null_StaysNull()
    {
        Assert.Null(SttConfidencePercent.FromNormalized((double?)null));
        Assert.Null(SttConfidencePercent.FromNormalized((float?)null));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void FromNormalized_NonFinite_MapsToNull_NotZero(double value)
    {
        Assert.Null(SttConfidencePercent.FromNormalized(value));
    }

    [Theory]
    [InlineData(0.85f, 85)]
    [InlineData(0.849f, 84)]
    public void FromNormalized_Float_DelegatesAndMatches(float normalized, int expectedPercent)
    {
        Assert.Equal(expectedPercent, SttConfidencePercent.FromNormalized(normalized));
    }

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(150, 100)]
    [InlineData(72, 72)]
    public void Clamp_StillClampsInteger(int input, int expected)
    {
        Assert.Equal(expected, SttConfidencePercent.Clamp(input));
    }
}
