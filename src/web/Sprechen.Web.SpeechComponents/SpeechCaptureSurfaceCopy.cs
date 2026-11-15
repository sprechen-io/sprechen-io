using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Web.SpeechComponents;

/// <summary>
/// Ermittelt sprachabhängige ARIA-Beschriftungen und Statusmeldungen für Aufnahme und Sprachaktivierung.
/// </summary>
public static class SpeechCaptureSurfaceCopy
{
    private const string WakeWordsPlaceholder = "{wakeWords}";

    private static readonly VoiceInterfaceLanguageCatalog LanguageCatalog = VoiceInterfaceLanguageCatalog.CreateDefault();

    public static string GetAriaLabel(SpeechCaptureSurfaceStateSnapshot state, string? isoCode)
    {
        ArgumentNullException.ThrowIfNull(state);

        return ResolveText(ResolveAriaLabelResourceId(state), isoCode);
    }

    public static string GetStatusAnnouncement(SpeechCaptureSurfaceStateSnapshot state, string? isoCode)
    {
        return GetStatusAnnouncement(state, isoCode, wakeWords: null);
    }

    public static string GetStatusAnnouncement(
        SpeechCaptureSurfaceStateSnapshot state,
        string? isoCode,
        string? wakeWords)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (MentionsWakeWords(state, wakeWords))
        {
            var template = ResolveText(VoiceInterfaceResourceIds.SpeechCaptureStatusReadyWithWakeWords, isoCode);
            return template.Replace(WakeWordsPlaceholder, wakeWords, StringComparison.Ordinal);
        }

        return ResolveText(ResolveStatusResourceId(state), isoCode);
    }

    private static bool MentionsWakeWords(SpeechCaptureSurfaceStateSnapshot state, string? wakeWords)
    {
        return !string.IsNullOrWhiteSpace(wakeWords)
            && !state.IsRecording
            && state.ActivationState == SpeechAssistantActivationState.Ready
            && !state.WakeListeningFallbackState.IsLexicalFallbackActive;
    }

    private static string ResolveAriaLabelResourceId(SpeechCaptureSurfaceStateSnapshot state)
    {
        if (state.IsRecording)
        {
            return VoiceInterfaceResourceIds.SpeechCaptureAriaLabelListening;
        }

        return state.ActivationState switch
        {
            SpeechAssistantActivationState.Ready when state.WakeListeningFallbackState.IsLexicalFallbackActive
                => VoiceInterfaceResourceIds.SpeechCaptureAriaLabelReadyFallback,
            SpeechAssistantActivationState.Starting => VoiceInterfaceResourceIds.SpeechCaptureAriaLabelStarting,
            SpeechAssistantActivationState.Paused => VoiceInterfaceResourceIds.SpeechCaptureAriaLabelPaused,
            SpeechAssistantActivationState.Ready => VoiceInterfaceResourceIds.SpeechCaptureAriaLabelReady,
            SpeechAssistantActivationState.Failed => VoiceInterfaceResourceIds.SpeechCaptureAriaLabelFailed,
            _ => VoiceInterfaceResourceIds.SpeechCaptureAriaLabelDefault
        };
    }

    private static string ResolveStatusResourceId(SpeechCaptureSurfaceStateSnapshot state)
    {
        if (state.IsRecording)
        {
            return VoiceInterfaceResourceIds.SpeechCaptureStatusListening;
        }

        return state.ActivationState switch
        {
            SpeechAssistantActivationState.Starting => VoiceInterfaceResourceIds.SpeechCaptureStatusStarting,
            SpeechAssistantActivationState.Ready when state.WakeListeningFallbackState.IsLexicalFallbackActive
                => ResolveLexicalFallbackResourceId(state.WakeListeningFallbackState),
            SpeechAssistantActivationState.Paused => VoiceInterfaceResourceIds.SpeechCaptureStatusPaused,
            SpeechAssistantActivationState.Ready => VoiceInterfaceResourceIds.SpeechCaptureStatusReady,
            SpeechAssistantActivationState.Failed => VoiceInterfaceResourceIds.SpeechCaptureStatusFailed,
            _ => VoiceInterfaceResourceIds.SpeechCaptureStatusDefault
        };
    }

    private static string ResolveLexicalFallbackResourceId(SpeechWakeListeningFallbackState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return state.Reason switch
        {
            LexicalWakeFallbackReason.OverrideIneligible => VoiceInterfaceResourceIds.SpeechCaptureLexicalFallbackOverrideIneligible,
            LexicalWakeFallbackReason.DetectorUnavailable => VoiceInterfaceResourceIds.SpeechCaptureLexicalFallbackDetectorUnavailable,
            LexicalWakeFallbackReason.DetectorWarmup => VoiceInterfaceResourceIds.SpeechCaptureLexicalFallbackDetectorWarmup,
            LexicalWakeFallbackReason.DetectorFailure => VoiceInterfaceResourceIds.SpeechCaptureLexicalFallbackDetectorFailure,
            LexicalWakeFallbackReason.RecognitionOnlyActivation => VoiceInterfaceResourceIds.SpeechCaptureLexicalFallbackRecognitionOnly,
            _ => VoiceInterfaceResourceIds.SpeechCaptureLexicalFallbackDefault
        };
    }

    private static string ResolveText(string resourceId, string? isoCode)
    {
        var displayIsoCode = ResolveDisplayLanguage(isoCode);
        if (!VoiceInterfaceModelCatalog.Resources.TryGetResourceText(resourceId, displayIsoCode, out var text))
        {
            throw new InvalidOperationException(
                $"Speech capture surface resource '{resourceId}' is missing text for '{displayIsoCode}'.");
        }

        return text;
    }

    private static string ResolveDisplayLanguage(string? isoCode)
    {
        if (!LanguageCode.TryParseIso6391(isoCode, out var language)
            || !LanguageCatalog.IsSupported(language))
        {
            return VoiceInterfaceModelCatalog.DefaultLanguage;
        }

        return language.Value;
    }
}
