namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Zuordnung von Telemetrieereignissen zu <see cref="TelemetryStudyChannelMembership"/>.
/// Jeder katalogisierte Ereignisname hat genau eine Zugehörigkeit. Die Zuordnung
/// beschreibt den vergleichbaren Ereignisumfang der HTML- und Sprachanwendung.
/// </summary>
public static class TelemetryStudyChannelMembershipCatalog
{
    private static readonly IReadOnlyDictionary<string, TelemetryStudyChannelMembership> MembershipByEvent =
        new Dictionary<string, TelemetryStudyChannelMembership>(StringComparer.Ordinal)
        {
            [TelemetryEventNames.TaskStart] = TelemetryStudyChannelMembership.BothChannels,
            [TelemetryEventNames.TaskEnd] = TelemetryStudyChannelMembership.BothChannels,
            [TelemetryEventNames.NavigationChanged] = TelemetryStudyChannelMembership.BothChannels,
            [TelemetryEventNames.FieldRelevanceChanged] = TelemetryStudyChannelMembership.BothChannels,
            [TelemetryEventNames.FieldCompletionChanged] = TelemetryStudyChannelMembership.BothChannels,
            [TelemetryEventNames.FieldGroupRelevanceChanged] = TelemetryStudyChannelMembership.BothChannels,
            [TelemetryEventNames.FieldGroupCompletionChanged] = TelemetryStudyChannelMembership.BothChannels,
            [TelemetryEventNames.StatusSnapshot] = TelemetryStudyChannelMembership.BothChannels,
            [TelemetryEventNames.ValidationErrorOpened] = TelemetryStudyChannelMembership.BothChannels,
            [TelemetryEventNames.ValidationErrorCleared] = TelemetryStudyChannelMembership.BothChannels,
            [TelemetryEventNames.NavigationError] = TelemetryStudyChannelMembership.BothChannels,
            [TelemetryEventNames.FocusChange] = TelemetryStudyChannelMembership.BothChannels,
            [TelemetryEventNames.ValueChange] = TelemetryStudyChannelMembership.BothChannels,
            [TelemetryEventNames.ManualStep] = TelemetryStudyChannelMembership.ObserverOnly,
            [TelemetryEventNames.AmbiguityResolved] = TelemetryStudyChannelMembership.BothChannels,

            [TelemetryEventNames.SttError] = TelemetryStudyChannelMembership.VoiceOnly,
            [TelemetryEventNames.IntentRecognized] = TelemetryStudyChannelMembership.VoiceOnly,
            [TelemetryEventNames.CommandRecognitionFailed] = TelemetryStudyChannelMembership.VoiceOnly,
            [TelemetryEventNames.NoSpeechDetected] = TelemetryStudyChannelMembership.VoiceOnly,
            [TelemetryEventNames.SystemFeedback] = TelemetryStudyChannelMembership.VoiceOnly,
            [TelemetryEventNames.InputModeChanged] = TelemetryStudyChannelMembership.VoiceOnly,
            [TelemetryEventNames.DestructiveCommandResolved] = TelemetryStudyChannelMembership.VoiceOnly,
            [TelemetryEventNames.BargeIn] = TelemetryStudyChannelMembership.VoiceOnly,
            [TelemetryEventNames.ConversationalPacingSignal] = TelemetryStudyChannelMembership.VoiceOnly,

            [TelemetryEventNames.WakeWordListenerSetup] = TelemetryStudyChannelMembership.NonComparableDiagnostic,
            [TelemetryEventNames.WakeWordAcousticDetection] = TelemetryStudyChannelMembership.NonComparableDiagnostic,
            [TelemetryEventNames.WakeWordCaptureLoop] = TelemetryStudyChannelMembership.NonComparableDiagnostic,
            [TelemetryEventNames.WakeWordTranscriptionRejection] = TelemetryStudyChannelMembership.NonComparableDiagnostic,
            [TelemetryEventNames.WakeStitch] = TelemetryStudyChannelMembership.NonComparableDiagnostic,
            [TelemetryEventNames.WakeWordMiss] = TelemetryStudyChannelMembership.NonComparableDiagnostic,
            [TelemetryEventNames.WakeWordActivation] = TelemetryStudyChannelMembership.NonComparableDiagnostic,
            [TelemetryEventNames.WakeWordFallback] = TelemetryStudyChannelMembership.NonComparableDiagnostic,
            [TelemetryEventNames.LanguageDetected] = TelemetryStudyChannelMembership.NonComparableDiagnostic,
        };

    /// <summary>
    /// Vollständige Zuordnung der katalogisierten Ereignisnamen zu ihrer Studienzugehörigkeit.
    /// </summary>
    public static IReadOnlyDictionary<string, TelemetryStudyChannelMembership> All => MembershipByEvent;

    /// <summary>
    /// Liefert die Studienzugehörigkeit eines katalogisierten Ereignisnamens.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Der Ereignisname ist nicht klassifiziert.</exception>
    public static TelemetryStudyChannelMembership Membership(string eventName)
    {
        ArgumentNullException.ThrowIfNull(eventName);
        return MembershipByEvent.TryGetValue(eventName, out var membership)
            ? membership
            : throw new KeyNotFoundException($"Telemetry event '{eventName}' has no study-channel membership.");
    }

    /// <summary>
    /// Liefert die Ereignisnamen mit der angegebenen Zugehörigkeit.
    /// </summary>
    public static IReadOnlySet<string> EventsWith(TelemetryStudyChannelMembership membership) =>
        MembershipByEvent
            .Where(pair => pair.Value == membership)
            .Select(pair => pair.Key)
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Ereignisse der Sprachoberfläche (B):
    /// <see cref="TelemetryStudyChannelMembership.VoiceOnly"/> und
    /// <see cref="TelemetryStudyChannelMembership.BothChannels"/>.
    /// </summary>
    public static IReadOnlySet<string> VoiceChannelEvents() =>
        Union(TelemetryStudyChannelMembership.VoiceOnly, TelemetryStudyChannelMembership.BothChannels);

    /// <summary>
    /// Nicht vergleichbare Diagnosen, welche die Sprachoberfläche (B) senden darf.
    /// Die ausdrückliche Freigabe bleibt von der bloßen Zugehörigkeit als Diagnose getrennt.
    /// </summary>
    public static IReadOnlySet<string> VoiceEmittedDiagnosticEvents() =>
        new HashSet<string>(StringComparer.Ordinal) { TelemetryEventNames.WakeWordMiss };

    /// <summary>
    /// Ereignisse des HTML-Referenzformulars (A):
    /// <see cref="TelemetryStudyChannelMembership.HtmlOnly"/> und
    /// <see cref="TelemetryStudyChannelMembership.BothChannels"/>.
    /// </summary>
    public static IReadOnlySet<string> HtmlChannelEvents() =>
        Union(TelemetryStudyChannelMembership.HtmlOnly, TelemetryStudyChannelMembership.BothChannels);

    private static IReadOnlySet<string> Union(
        TelemetryStudyChannelMembership first,
        TelemetryStudyChannelMembership second)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        result.UnionWith(EventsWith(first));
        result.UnionWith(EventsWith(second));
        return result;
    }
}
