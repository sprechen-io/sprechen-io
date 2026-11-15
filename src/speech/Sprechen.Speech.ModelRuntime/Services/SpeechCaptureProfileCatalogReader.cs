using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.RuntimeArtifacts;
using Sprechen.Mda.Contracts.V1.SpeechCapture;

namespace Sprechen.Speech.ModelRuntime.Services;

/// <summary>
/// Liest die modellierten Aufnahmezeitvorgaben aus dem eingebetteten Referenzvertrag
/// speech-capture-profile.reference.json und stellt sie nach Aufnahmemodus bereit.
/// </summary>
public static class SpeechCaptureProfileCatalogReader
{
    private const string CatalogPath = "dialogmodell/reference-contracts/voice-interface/speech-capture-profile.reference.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Lazy<IReadOnlyDictionary<SpeechCaptureMode, SpeechCaptureProfile>> ProfilesByMode =
        new(LoadProfiles);

    private static readonly Lazy<SpeechCaptureProfileCatalog> Catalog = new(LoadCatalog);

    /// <summary>
    /// Liefert das modellierte Mikrofonsperrprofil oder null, wenn es fehlt.
    /// </summary>
    public static SpeechCaptureMicGateProfile? GetMicGateProfile() => Catalog.Value.MicGate;

    /// <summary>
    /// Liefert das modellierte Segmentiererprofil oder null, wenn es fehlt.
    /// </summary>
    public static SpeechCaptureSegmenterProfile? GetSegmenterProfile() => Catalog.Value.Segmenter;

    /// <summary>
    /// Erste Wartefrist auf Sprache für Felder mit vorgelesenen Auswahlmöglichkeiten.
    /// Null, wenn keine positive Vorgabe modelliert ist.
    /// </summary>
    public static int? GetOptionListingInitialSilenceTimeoutMs()
    {
        var value = Catalog.Value.Segmenter?.OptionListingInitialSilenceTimeoutMs;
        return value > 0 ? value : null;
    }

    /// <summary>
    /// Liefert die modellierten Zeitfenster der Sprachwahl beim Sitzungseinstieg.
    /// Der erforderliche Block wird bereits beim Katalogladen geprüft. Hier gibt es keinen Ersatzwert.
    /// </summary>
    public static SpeechCaptureSessionEntryProfile GetSessionEntryProfile() => Catalog.Value.SessionEntry;

    /// <summary>
    /// Modellierte Stillefrist in Millisekunden für den Aufnahmemodus. Null bei fehlender Vorgabe.
    /// </summary>
    public static int? GetSilenceDebounceMs(SpeechCaptureMode mode)
        => ProfilesByMode.Value.TryGetValue(mode, out var profile) ? profile.SilenceDebounceMs : null;

    /// <summary>
    /// Modellierte maximale Äußerungsdauer für den Aufnahmemodus. Null bei fehlender Vorgabe.
    /// </summary>
    public static TimeSpan? GetMaxUtteranceDuration(SpeechCaptureMode mode)
        => ProfilesByMode.Value.TryGetValue(mode, out var profile)
            && profile.MaxUtteranceDurationMs is { } ms
            ? TimeSpan.FromMilliseconds(ms)
            : null;

    /// <summary>
    /// Modellierte Gesamtdauergrenze aus Sprache und Stille für den Aufnahmemodus.
    /// Null bei fehlender Vorgabe.
    /// </summary>
    public static TimeSpan? GetMaxTotalSegmentDuration(SpeechCaptureMode mode)
        => ProfilesByMode.Value.TryGetValue(mode, out var profile)
            && profile.MaxTotalSegmentDurationMs is { } ms
            ? TimeSpan.FromMilliseconds(ms)
            : null;

    /// <summary>
    /// Modellierte Dauer zur Bestätigung eines Sprachbeginns für den Aufnahmemodus.
    /// Null bei fehlender Vorgabe.
    /// </summary>
    public static TimeSpan? GetSpeechStartConfirmation(SpeechCaptureMode mode)
        => ProfilesByMode.Value.TryGetValue(mode, out var profile)
            && profile.SpeechStartConfirmationMs is { } ms
            ? TimeSpan.FromMilliseconds(ms)
            : null;

    public static IReadOnlyCollection<SpeechCaptureMode> GetModeledModes() => ProfilesByMode.Value.Keys.ToArray();

    private static IReadOnlyDictionary<SpeechCaptureMode, SpeechCaptureProfile> LoadProfiles()
    {
        var catalog = Catalog.Value;

        var result = new Dictionary<SpeechCaptureMode, SpeechCaptureProfile>();
        foreach (var profile in catalog.CaptureProfilesByMode)
        {
            if (!Enum.TryParse<SpeechCaptureMode>(profile.Mode, ignoreCase: false, out var parsed)
                || !Enum.IsDefined(parsed))
            {
                throw new InvalidOperationException(
                    $"Speech-capture-profile mode '{profile.Mode}' is not a known SpeechCaptureMode value.");
            }

            result[parsed] = profile;
        }

        return result;
    }

    private static SpeechCaptureProfileCatalog LoadCatalog()
    {
        var json = MdaEmbeddedRuntimeArtifacts.ReadAllBytes(CatalogPath);
        var catalog = JsonSerializer.Deserialize<SpeechCaptureProfileCatalog>(json, SerializerOptions)
            ?? throw new InvalidOperationException($"Embedded speech-capture-profile catalog '{CatalogPath}' could not be deserialized.");

        var preflight = SpeechCaptureProfileCatalogPreflight.Validate(catalog);
        if (!preflight.Ok)
        {
            var details = string.Join("; ", preflight.Errors.Select(error => $"{error.Path}:{error.Message}"));
            throw new InvalidOperationException($"Embedded speech-capture-profile catalog preflight failed: {details}");
        }

        return catalog;
    }
}
