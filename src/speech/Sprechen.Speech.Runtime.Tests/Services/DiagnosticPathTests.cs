namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class DiagnosticPathTests
{
    [Theory]
    [InlineData("/debug")]
    [InlineData("/DEBUG")]
    [InlineData("/Debug")]
    [InlineData("/debug/umfrage/x9jdln")]
    [InlineData("/debug/umfrage/probe/aufgabe/t05")]
    public void DebugPath_TurnsTheModeOn(string absolutePath)
    {
        Assert.True(DiagnosticPath.IsMatch(absolutePath));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/home")]
    [InlineData("/debug/extra")]
    public void HomeAndUnknownPaths_LeaveTheModeOff(string absolutePath)
    {
        Assert.False(DiagnosticPath.IsMatch(absolutePath));
    }

    [Theory]
    [InlineData("/umfrage/probe")]
    [InlineData("/umfrage/probe/aufgabe/t05")]
    [InlineData("/umfrage/probe/variante/X")]
    [InlineData("/umfrage/probe/aufgabe/t05/variante/Y")]
    public void StudyPaths_StayUnchanged(string absolutePath)
    {
        Assert.False(DiagnosticPath.IsMatch(absolutePath));
    }
}
