using System.Runtime.CompilerServices;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests;

internal static class MicrophoneJsCompatibilityInitializer
{
    [ModuleInitializer]
    internal static void RegisterLegacyMicrophoneAdapter()
    {
        MicrophoneJsTestBridge.Adapt = static runtime => new LegacyMicrophoneJsAdapter(runtime);
    }
}
