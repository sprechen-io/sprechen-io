using Bunit;
using Microsoft.JSInterop;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class BrowserDebugConsoleTests : TestContext
{
    [Fact]
    public async Task GetEnabledAsync_WhenCalled_ReturnsValueFromJs()
    {
        JSInterop.Setup<bool>("browserDebugConsole.getEnabled")
            .SetResult(true);

        var console = new BrowserDebugConsole(JSInterop.JSRuntime);

        var enabled = await console.GetEnabledAsync(CancellationToken.None);

        Assert.True(enabled);
    }

    [Fact]
    public async Task SetEnabledAsync_WhenCalled_InvokesJs()
    {
        JSInterop.SetupVoid("browserDebugConsole.setEnabled", invocation => invocation.Arguments.Count == 1)
            .SetVoidResult();

        var console = new BrowserDebugConsole(JSInterop.JSRuntime);

        await console.SetEnabledAsync(true, CancellationToken.None);

        JSInterop.VerifyInvoke("browserDebugConsole.setEnabled");
    }

    [Fact]
    public async Task WriteAsync_WhenDisabled_DoesNotInvokeWriteBridge()
    {
        JSInterop.Setup<bool>("browserDebugConsole.getEnabled")
            .SetResult(false);

        var console = new BrowserDebugConsole(JSInterop.JSRuntime);

        await console.WriteAsync("session-setup", "message", CancellationToken.None);

        Assert.DoesNotContain(
            JSInterop.Invocations,
            invocation => string.Equals(invocation.Identifier, "browserDebugConsole.write", StringComparison.Ordinal));
    }

    [Fact]
    public async Task WriteAsync_WhenDiagnosticSessionIsOnAndTheFlagIsOff_InvokesWriteBridge()
    {
        JSInterop.Setup<bool>("browserDebugConsole.getEnabled")
            .SetResult(false);
        JSInterop.SetupVoid("browserDebugConsole.setDiagnosticSession", _ => true)
            .SetVoidResult();
        JSInterop.SetupVoid("browserDebugConsole.write", invocation => invocation.Arguments.Count == 2)
            .SetVoidResult();

        var console = new BrowserDebugConsole(JSInterop.JSRuntime);
        await console.SetDiagnosticSessionAsync(
            new DiagnosticSessionState(true, "sid", "cid", DiagnosticOperatingModeNames.Diagnostic),
            CancellationToken.None);

        await console.WriteAsync("session-setup", "message", CancellationToken.None);

        JSInterop.VerifyInvoke("browserDebugConsole.write");
    }

    [Fact]
    public async Task WriteAsync_WhenEnabled_InvokesWriteBridge()
    {
        JSInterop.Setup<bool>("browserDebugConsole.getEnabled")
            .SetResult(true);
        JSInterop.SetupVoid("browserDebugConsole.write", invocation => invocation.Arguments.Count == 2)
            .SetVoidResult();

        var console = new BrowserDebugConsole(JSInterop.JSRuntime);

        await console.WriteAsync("session-setup", "message", CancellationToken.None);

        JSInterop.VerifyInvoke("browserDebugConsole.write");
    }
}
