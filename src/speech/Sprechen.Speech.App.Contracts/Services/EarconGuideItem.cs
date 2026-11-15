namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Verbindet einen Rückmeldungsklang mit seiner Beschreibung für die Erklärung im Klangführer.
/// </summary>
public sealed record EarconGuideItem(
    EarconId EarconId,
    string Description);
