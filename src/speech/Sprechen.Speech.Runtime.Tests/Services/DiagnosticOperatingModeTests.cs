using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class DiagnosticOperatingModeTests
{
    [Fact]
    public void ANewMode_IsNotDiagnostic()
    {
        var console = new InMemoryBrowserDebugConsole();
        var mode = new DiagnosticOperatingMode(new FixedSpeechSessionIdentity("sid"), console);

        Assert.False(mode.IsDiagnostic);
        Assert.Equal(DiagnosticOperatingModeNames.Standard, mode.OperatingModeMarker);
    }

    [Fact]
    public void ActivatingDiagnosticMode_OpensTheConsoleSessionAndDoesNotSurviveANewInstance()
    {
        var console = new InMemoryBrowserDebugConsole();
        var first = new DiagnosticOperatingMode(new FixedSpeechSessionIdentity("sid"), console);
        first.Activate(true);

        Assert.True(first.IsDiagnostic);
        Assert.Equal("sid", first.SessionId);
        Assert.Equal(DiagnosticOperatingModeNames.Diagnostic, first.OperatingModeMarker);
        Assert.True(console.DiagnosticSession);
        Assert.Equal("sid", console.LastDiagnosticSession?.SessionId);

        var second = new DiagnosticOperatingMode(new FixedSpeechSessionIdentity("sid-2"), new InMemoryBrowserDebugConsole());
        Assert.False(second.IsDiagnostic);
    }

    [Fact]
    public void Deactivating_ClearsTheConsoleSession()
    {
        var console = new InMemoryBrowserDebugConsole();
        var mode = new DiagnosticOperatingMode(new FixedSpeechSessionIdentity("sid"), console);
        mode.Activate(true);
        mode.Activate(false);

        Assert.False(mode.IsDiagnostic);
        Assert.False(console.DiagnosticSession);
    }
}
