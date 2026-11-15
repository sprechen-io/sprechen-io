using Sprechen.Speech.ModelRuntime.Services;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Bestimmt die Reihenfolge der angebotenen Sprachen unabhängig von deren Annahmeregeln. Die
/// Sitzungssprache steht zuerst, die übrigen folgen der Katalogreihenfolge, damit der Ablauf keine
/// eigene Gewichtung der Sprachen einführt.
/// </summary>
internal static class SessionEntryLadder
{
    /// <summary>
    /// Liefert die Sitzungssprache zuerst und anschließend die übrigen Katalogsprachen.
    /// </summary>
    public static IReadOnlyList<string> Order(string sessionIsoCode)
        => [sessionIsoCode, .. Others(sessionIsoCode)];

    /// <summary>
    /// Liefert die unterstützten Katalogsprachen außer der aktuellen Sitzungssprache in ihrer
    /// vorhandenen Reihenfolge.
    /// </summary>
    public static IReadOnlyList<string> Others(string sessionIsoCode)
        => [.. VoiceInterfaceModelCatalog.SupportedLanguages
            .Where(iso => !string.Equals(iso, sessionIsoCode, StringComparison.OrdinalIgnoreCase))];
}
