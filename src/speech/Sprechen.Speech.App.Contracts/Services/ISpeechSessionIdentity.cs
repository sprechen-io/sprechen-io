namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Gemeinsame Sitzungskennung für Synthese- und Erkennungsanfragen,
/// um Ansagen und Erkennungen derselben Sitzung zuordnen zu können.
/// </summary>
public interface ISpeechSessionIdentity
{
    /// <summary>Undurchsichtige Kennung, die während der Browsersitzung stabil bleibt.</summary>
    string SessionId { get; }
}
