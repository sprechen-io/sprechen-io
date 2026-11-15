namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class FixedSpeechSessionIdentity(string sessionId) : ISpeechSessionIdentity
{
    public string SessionId { get; } = sessionId;
}
