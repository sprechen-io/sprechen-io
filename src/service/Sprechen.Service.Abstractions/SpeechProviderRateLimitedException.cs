namespace Sprechen.Service.Abstractions;

/// <summary>
/// Kennzeichnet eine Ratenbegrenzung des Sprachdienstanbieters mit Anbieter- und Operationsname.
/// </summary>
public sealed class SpeechProviderRateLimitedException : Exception
{
    public string Provider { get; }

    public string Operation { get; }

    public SpeechProviderRateLimitedException(string provider, string operation)
        : this(provider, operation, innerException: null)
    {
    }

    public SpeechProviderRateLimitedException(string provider, string operation, Exception? innerException)
        : base(CreateMessage(provider, operation), innerException)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ArgumentException("Provider is required.", nameof(provider));
        }

        if (string.IsNullOrWhiteSpace(operation))
        {
            throw new ArgumentException("Operation is required.", nameof(operation));
        }

        Provider = provider.Trim();
        Operation = operation.Trim();
    }

    private static string CreateMessage(string provider, string operation)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(operation))
        {
            return "Speech provider rate limited.";
        }

        return $"Speech provider rate limited (provider={provider.Trim()}, operation={operation.Trim()}).";
    }
}
