namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Bündelt Sprache, Einführung und Klangeinträge für die Darstellung des Klangführers.
/// </summary>
public sealed record EarconGuideState(
    string SelectedIsoCode,
    string IntroText,
    IReadOnlyList<EarconGuideItem> Items);
