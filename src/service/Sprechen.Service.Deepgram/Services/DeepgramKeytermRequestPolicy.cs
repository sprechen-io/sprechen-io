namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Prüft die zusammengestellte Liste von Erkennungsbegriffen gegen die Obergrenze von 100 Einträgen.
/// </summary>
internal sealed class DeepgramKeytermRequestPolicy
{
    internal const int MaximumCount = 100;

    internal void Validate(IReadOnlyList<string> keyterms)
    {
        ArgumentNullException.ThrowIfNull(keyterms);

        if (keyterms.Count > MaximumCount)
        {
            throw new InvalidOperationException(
                $"Deepgram keyterm composition exceeds the provider limit of {MaximumCount} entries.");
        }
    }
}
