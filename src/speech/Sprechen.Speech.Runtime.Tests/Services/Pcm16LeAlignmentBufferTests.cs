namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class Pcm16LeAlignmentBufferTests
{
    [Fact]
    public void Align_CarriesOddTrailingByteIntoNextFrame()
    {
        var buffer = new Pcm16LeAlignmentBuffer();

        var first = buffer.Align([1, 2, 3]);
        var second = buffer.Align([4, 5]);
        var third = buffer.Align([6]);

        Assert.Equal([1, 2], first);
        Assert.Equal([3, 4], second);
        Assert.Equal([5, 6], third);
    }

    [Fact]
    public void Align_WhenSingleOddByte_ReturnsEmptyUntilPairCompletes()
    {
        var buffer = new Pcm16LeAlignmentBuffer();

        var first = buffer.Align([1]);
        var second = buffer.Align([2]);

        Assert.Empty(first);
        Assert.Equal([1, 2], second);
    }
}
