namespace Sprechen.Speech.Runtime.Tests.Capture;

public sealed class ActiveCaptureBacklogReadResultTests
{
    [Fact]
    public void ActiveCaptureBacklogReadResult_Inactive_ReturnsInactiveDefaultUpdate()
    {
        var result = ActiveCaptureBacklogReadResult.Inactive;

        Assert.False(result.IsActive);
        Assert.Equal(default, result.Update);
    }
}
