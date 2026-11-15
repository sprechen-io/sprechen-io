namespace Sprechen.Speech.LanguageContracts;

/// <summary>
/// Liest die vom Browser bevorzugten Sprachkennungen für die Sprachwahl aus.
/// </summary>
public interface IBrowserLanguageProvider
{
    ValueTask<IReadOnlyList<string>> GetPreferredLanguageTagsAsync(CancellationToken cancellationToken);
}
