using Sprechen.Service.Deepgram.Services;
using Xunit;

namespace Sprechen.Service.Deepgram.Tests.Services;

public sealed class DeepgramSdkInitializerTests
{
    [Fact]
    public void EnsureInitialized_CanBeCalledMultipleTimes()
    {
        DeepgramSdkInitializer.EnsureInitialized();
        DeepgramSdkInitializer.EnsureInitialized();
    }
}

