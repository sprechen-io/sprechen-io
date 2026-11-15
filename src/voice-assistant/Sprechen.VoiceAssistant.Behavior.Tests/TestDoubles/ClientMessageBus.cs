using Microsoft.Extensions.Logging;

namespace Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

internal sealed class ClientMessageBus(ILogger<ClientMessageBus> logger)
{
    public ILogger<ClientMessageBus> Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));
}
