using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Bildet LexicalWakeFallbackReason auf zwei verschiedene Arten ab:
/// ToWireCode und TryParseWireCode verwenden die festen camelCase-Schlüssel des Schemas.
/// ResolveCatalogReasonCode und TryFromCatalogReasonCode verwenden die frei wählbaren
/// Werte aus dem geladenen Katalog. Ein Schemaschlüssel ist deshalb nicht mit dem
/// zugehörigen Katalogwert gleichzusetzen.
/// </summary>
public static class LexicalWakeFallbackReasonExtensions
{
    public const string CatalogFallbackOnlyWireKey = "catalogFallbackOnly";

    public const string OverrideIneligibleWireKey = "overrideIneligible";

    public const string RecognitionOnlyActivationWireKey = "recognitionOnlyActivation";

    public const string DetectorUnavailableWireKey = "detectorUnavailable";

    public const string DetectorWarmupWireKey = "detectorWarmup";

    public const string DetectorFailureWireKey = "detectorFailure";

    /// <summary>
    /// Liefert den festen camelCase-Schlüssel des Schemas.
    /// None und unbekannte Enum-Werte führen zu ArgumentOutOfRangeException.
    /// </summary>
    public static string ToWireCode(this LexicalWakeFallbackReason reason)
    {
        return reason switch
        {
            LexicalWakeFallbackReason.CatalogFallbackOnly => CatalogFallbackOnlyWireKey,
            LexicalWakeFallbackReason.OverrideIneligible => OverrideIneligibleWireKey,
            LexicalWakeFallbackReason.RecognitionOnlyActivation => RecognitionOnlyActivationWireKey,
            LexicalWakeFallbackReason.DetectorUnavailable => DetectorUnavailableWireKey,
            LexicalWakeFallbackReason.DetectorWarmup => DetectorWarmupWireKey,
            LexicalWakeFallbackReason.DetectorFailure => DetectorFailureWireKey,
            LexicalWakeFallbackReason.None => throw new ArgumentOutOfRangeException(
                nameof(reason),
                reason,
                "LexicalWakeFallbackReason.None is a sentinel meaning 'no fallback active' and must never be projected to a wire string."),
            _ => throw new ArgumentOutOfRangeException(
                nameof(reason),
                reason,
                $"Unknown LexicalWakeFallbackReason value '{(int)reason}'."),
        };
    }

    /// <summary>
    /// Ordnet einen festen Schemaschlüssel dem Enum zu, mit exaktem ordinalem Vergleich.
    /// Bei null, Leerraum oder unbekanntem Schlüssel ist das Ergebnis false und reason wird None.
    /// </summary>
    public static bool TryParseWireCode(string? wireCode, out LexicalWakeFallbackReason reason)
    {
        if (string.IsNullOrWhiteSpace(wireCode))
        {
            reason = LexicalWakeFallbackReason.None;
            return false;
        }

        switch (wireCode)
        {
            case CatalogFallbackOnlyWireKey:
                reason = LexicalWakeFallbackReason.CatalogFallbackOnly;
                return true;
            case OverrideIneligibleWireKey:
                reason = LexicalWakeFallbackReason.OverrideIneligible;
                return true;
            case RecognitionOnlyActivationWireKey:
                reason = LexicalWakeFallbackReason.RecognitionOnlyActivation;
                return true;
            case DetectorUnavailableWireKey:
                reason = LexicalWakeFallbackReason.DetectorUnavailable;
                return true;
            case DetectorWarmupWireKey:
                reason = LexicalWakeFallbackReason.DetectorWarmup;
                return true;
            case DetectorFailureWireKey:
                reason = LexicalWakeFallbackReason.DetectorFailure;
                return true;
            default:
                reason = LexicalWakeFallbackReason.None;
                return false;
        }
    }

    /// <summary>
    /// Liest den zum Enum gehörenden Wert aus dem übergebenen Katalog.
    /// Dieser erscheint in AcousticWakeWordDetectorConfiguration.LexicalFallbackReasonCode
    /// und SpeechWakeListeningFallbackState.ReasonCode. None und unbekannte Enum-Werte
    /// führen zu ArgumentOutOfRangeException, ein fehlender Katalog zu ArgumentNullException.
    /// </summary>
    public static string ResolveCatalogReasonCode(
        this LexicalWakeFallbackReason reason,
        WakeWordDetectorFallbackReasonCodes catalogReasonCodes)
    {
        ArgumentNullException.ThrowIfNull(catalogReasonCodes);

        return reason switch
        {
            LexicalWakeFallbackReason.CatalogFallbackOnly => catalogReasonCodes.CatalogFallbackOnly,
            LexicalWakeFallbackReason.OverrideIneligible => catalogReasonCodes.OverrideIneligible,
            LexicalWakeFallbackReason.RecognitionOnlyActivation => catalogReasonCodes.RecognitionOnlyActivation,
            LexicalWakeFallbackReason.DetectorUnavailable => catalogReasonCodes.DetectorUnavailable,
            LexicalWakeFallbackReason.DetectorWarmup => catalogReasonCodes.DetectorWarmup,
            LexicalWakeFallbackReason.DetectorFailure => catalogReasonCodes.DetectorFailure,
            LexicalWakeFallbackReason.None => throw new ArgumentOutOfRangeException(
                nameof(reason),
                reason,
                "LexicalWakeFallbackReason.None is a sentinel meaning 'no fallback active' and must never be projected to a catalog wire string."),
            _ => throw new ArgumentOutOfRangeException(
                nameof(reason),
                reason,
                $"Unknown LexicalWakeFallbackReason value '{(int)reason}'."),
        };
    }

    /// <summary>
    /// Vergleicht reasonCode ordinal mit den Werten des übergebenen Katalogs, damit
    /// die Gründe unabhängig von dessen frei gewählten Zeichenketten auflösbar bleiben.
    /// Bei null, Leerraum oder fehlendem Treffer ist das Ergebnis false und reason wird None.
    /// Ein fehlender Katalog führt zu ArgumentNullException.
    /// </summary>
    public static bool TryFromCatalogReasonCode(
        string? reasonCode,
        WakeWordDetectorFallbackReasonCodes catalogReasonCodes,
        out LexicalWakeFallbackReason reason)
    {
        ArgumentNullException.ThrowIfNull(catalogReasonCodes);

        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            reason = LexicalWakeFallbackReason.None;
            return false;
        }

        if (string.Equals(reasonCode, catalogReasonCodes.CatalogFallbackOnly, StringComparison.Ordinal))
        {
            reason = LexicalWakeFallbackReason.CatalogFallbackOnly;
            return true;
        }

        if (string.Equals(reasonCode, catalogReasonCodes.OverrideIneligible, StringComparison.Ordinal))
        {
            reason = LexicalWakeFallbackReason.OverrideIneligible;
            return true;
        }

        if (string.Equals(reasonCode, catalogReasonCodes.RecognitionOnlyActivation, StringComparison.Ordinal))
        {
            reason = LexicalWakeFallbackReason.RecognitionOnlyActivation;
            return true;
        }

        if (string.Equals(reasonCode, catalogReasonCodes.DetectorUnavailable, StringComparison.Ordinal))
        {
            reason = LexicalWakeFallbackReason.DetectorUnavailable;
            return true;
        }

        if (string.Equals(reasonCode, catalogReasonCodes.DetectorWarmup, StringComparison.Ordinal))
        {
            reason = LexicalWakeFallbackReason.DetectorWarmup;
            return true;
        }

        if (string.Equals(reasonCode, catalogReasonCodes.DetectorFailure, StringComparison.Ordinal))
        {
            reason = LexicalWakeFallbackReason.DetectorFailure;
            return true;
        }

        reason = LexicalWakeFallbackReason.None;
        return false;
    }
}
