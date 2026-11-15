namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class DateComponentWorkflowBufferTests
{
    [Theory]
    [InlineData("de", new[] { DateComponentSlot.Day, DateComponentSlot.Month, DateComponentSlot.Year })]
    [InlineData("fr", new[] { DateComponentSlot.Day, DateComponentSlot.Month, DateComponentSlot.Year })]
    [InlineData("en", new[] { DateComponentSlot.Month, DateComponentSlot.Day, DateComponentSlot.Year })]
    [InlineData("en-US", new[] { DateComponentSlot.Month, DateComponentSlot.Day, DateComponentSlot.Year })]
    public void Order_FollowsTheLocalesSpokenOrder(string isoCode, DateComponentSlot[] expected)
    {
        var buffer = new DateComponentWorkflowBuffer(isoCode);

        Assert.Equal(expected, buffer.Order);
    }

    [Fact]
    public void Record_CollectsEachSlotOnce_AndAssemblesTheIsoDate()
    {
        var buffer = new DateComponentWorkflowBuffer("de");
        Assert.False(buffer.TryAssembleIsoDate(out _));

        buffer.Record(DateComponentSlot.Day, 24, 90);
        Assert.True(buffer.IsCollected(DateComponentSlot.Day));
        Assert.False(buffer.IsCollected(DateComponentSlot.Month));
        Assert.False(buffer.TryAssembleIsoDate(out _));

        buffer.Record(DateComponentSlot.Month, 12, 80);
        buffer.Record(DateComponentSlot.Year, 2025, 70);

        Assert.True(buffer.TryAssembleIsoDate(out var isoDate));
        Assert.Equal("2025-12-24", isoDate);
    }

    [Fact]
    public void Record_InEnglishOrder_AssemblesTheSameIsoDate()
    {
        var buffer = new DateComponentWorkflowBuffer("en");
        buffer.Record(DateComponentSlot.Month, 2, 80);
        buffer.Record(DateComponentSlot.Day, 28, 90);
        buffer.Record(DateComponentSlot.Year, 2024, 70);

        Assert.True(buffer.TryAssembleIsoDate(out var isoDate));
        Assert.Equal("2024-02-28", isoDate);
    }

    [Fact]
    public void GetSurvivingMinimumConfidencePercent_IsTheMinimumOverTheCollectedSlots()
    {
        var buffer = new DateComponentWorkflowBuffer("de");
        buffer.Record(DateComponentSlot.Day, 24, 90);
        buffer.Record(DateComponentSlot.Month, 12, 60);
        buffer.Record(DateComponentSlot.Year, 2025, 70);

        Assert.Equal(60, buffer.GetSurvivingMinimumConfidencePercent());
    }

    [Fact]
    public void GetSurvivingMinimumConfidencePercent_IgnoresASlotWithoutAReportedConfidence()
    {
        var buffer = new DateComponentWorkflowBuffer("de");
        buffer.Record(DateComponentSlot.Day, 24, null);
        buffer.Record(DateComponentSlot.Month, 12, null);

        Assert.Null(buffer.GetSurvivingMinimumConfidencePercent());

        buffer.Record(DateComponentSlot.Year, 2025, 70);

        Assert.Equal(70, buffer.GetSurvivingMinimumConfidencePercent());
    }

    [Fact]
    public void AnImpossibleDate_DoesNotAssemble_AndDiscardingTheDayKeepsMonthAndYear()
    {
        var buffer = new DateComponentWorkflowBuffer("de");
        buffer.Record(DateComponentSlot.Day, 31, 40);
        buffer.Record(DateComponentSlot.Month, 2, 80);
        buffer.Record(DateComponentSlot.Year, 2025, 90);

        Assert.False(buffer.TryAssembleIsoDate(out _));

        buffer.DiscardDay();

        Assert.False(buffer.IsCollected(DateComponentSlot.Day));
        Assert.True(buffer.IsCollected(DateComponentSlot.Month));
        Assert.True(buffer.IsCollected(DateComponentSlot.Year));
        Assert.Equal(80, buffer.GetSurvivingMinimumConfidencePercent());

        buffer.Record(DateComponentSlot.Day, 28, 50);

        Assert.True(buffer.TryAssembleIsoDate(out var isoDate));
        Assert.Equal("2025-02-28", isoDate);
        Assert.Equal(50, buffer.GetSurvivingMinimumConfidencePercent());
    }

    [Theory]
    [InlineData(0, 1, 2025)]
    [InlineData(32, 1, 2025)]
    [InlineData(1, 0, 2025)]
    [InlineData(1, 13, 2025)]
    [InlineData(1, 1, 999)]
    [InlineData(1, 1, 10000)]
    public void TryAssembleIsoDate_RejectsPartsOutOfRange(int day, int month, int year)
    {
        var buffer = new DateComponentWorkflowBuffer("de");
        buffer.Record(DateComponentSlot.Day, day, 90);
        buffer.Record(DateComponentSlot.Month, month, 90);
        buffer.Record(DateComponentSlot.Year, year, 90);

        Assert.False(buffer.TryAssembleIsoDate(out var isoDate));
        Assert.Equal(string.Empty, isoDate);
    }
}
