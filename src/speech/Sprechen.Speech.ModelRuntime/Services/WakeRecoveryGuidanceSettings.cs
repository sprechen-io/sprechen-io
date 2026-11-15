namespace Sprechen.Speech.ModelRuntime.Services;

/// <summary>
/// Modellierte Schwellen und Wartezeiten für gesprochene Hilfe nach fehlgeschlagener Aktivierung.
/// Die drei Werte stammen gemeinsam aus recoveryFeedback des Aktivierungswortkatalogs.
/// Fehlt der Block, liefert der Katalog null und die Laufzeit verwendet nur Hinweistöne.
/// </summary>
public sealed record WakeRecoveryGuidanceSettings(int MissThreshold, int CooldownSeconds, int MaxCooldownSeconds);
