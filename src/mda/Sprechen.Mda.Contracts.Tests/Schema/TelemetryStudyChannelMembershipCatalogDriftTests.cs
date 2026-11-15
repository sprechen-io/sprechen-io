using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.Mda.Contracts.Tests.Schema;

public sealed class TelemetryStudyChannelMembershipCatalogDriftTests
{
    [Fact]
    public void Catalog_Keys_MatchTelemetryEventNames()
    {
        var catalogEvents = TelemetryStudyChannelMembershipCatalog.All.Keys
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(
            catalogEvents.SetEquals(TelemetryEventNames.All),
            "Catalog keys must equal TelemetryEventNames.All.");
    }

    [Fact]
    public void Catalog_VoiceEmittedDiagnostics_AreClassifiedAsNonComparableDiagnostics()
    {
        var diagnostics = TelemetryStudyChannelMembershipCatalog
            .EventsWith(TelemetryStudyChannelMembership.NonComparableDiagnostic);

        var voiceEmitted = TelemetryStudyChannelMembershipCatalog.VoiceEmittedDiagnosticEvents();
        var misclassified = voiceEmitted.Except(diagnostics, StringComparer.Ordinal).OrderBy(x => x).ToArray();

        Assert.NotEmpty(voiceEmitted);
        Assert.True(
            misclassified.Length == 0,
            $"Only non-comparable diagnostics may be listed as voice-emitted diagnostics. Misclassified: [{string.Join(", ", misclassified)}]");
    }

    [Fact]
    public void Catalog_Membership_IsDisjoint_AcrossCategories()
    {
        var both = TelemetryStudyChannelMembershipCatalog.EventsWith(TelemetryStudyChannelMembership.BothChannels);
        var voice = TelemetryStudyChannelMembershipCatalog.EventsWith(TelemetryStudyChannelMembership.VoiceOnly);
        var html = TelemetryStudyChannelMembershipCatalog.EventsWith(TelemetryStudyChannelMembership.HtmlOnly);
        var observer = TelemetryStudyChannelMembershipCatalog.EventsWith(TelemetryStudyChannelMembership.ObserverOnly);
        var diagnostic = TelemetryStudyChannelMembershipCatalog.EventsWith(TelemetryStudyChannelMembership.NonComparableDiagnostic);

        var total = both.Count + voice.Count + html.Count + observer.Count + diagnostic.Count;
        var union = new HashSet<string>(StringComparer.Ordinal);
        union.UnionWith(both);
        union.UnionWith(voice);
        union.UnionWith(html);
        union.UnionWith(observer);
        union.UnionWith(diagnostic);

        Assert.Equal(union.Count, total);
        Assert.Equal(TelemetryStudyChannelMembershipCatalog.All.Count, total);
    }

    [Theory]
    [MemberData(nameof(ExpectedMemberships))]
    public void Catalog_Membership_MatchesExpected(string eventName, TelemetryStudyChannelMembership expected)
    {
        Assert.Equal(expected, TelemetryStudyChannelMembershipCatalog.Membership(eventName));
    }

    public static IEnumerable<object[]> ExpectedMemberships() =>
        new List<object[]>
        {
            new object[] { "task_start", TelemetryStudyChannelMembership.BothChannels },
            new object[] { "task_end", TelemetryStudyChannelMembership.BothChannels },
            new object[] { "navigation_changed", TelemetryStudyChannelMembership.BothChannels },
            new object[] { "field_relevance_changed", TelemetryStudyChannelMembership.BothChannels },
            new object[] { "field_completion_changed", TelemetryStudyChannelMembership.BothChannels },
            new object[] { "field_group_relevance_changed", TelemetryStudyChannelMembership.BothChannels },
            new object[] { "field_group_completion_changed", TelemetryStudyChannelMembership.BothChannels },
            new object[] { "status_snapshot", TelemetryStudyChannelMembership.BothChannels },
            new object[] { "validation_error_opened", TelemetryStudyChannelMembership.BothChannels },
            new object[] { "validation_error_cleared", TelemetryStudyChannelMembership.BothChannels },
            new object[] { "navigation_error", TelemetryStudyChannelMembership.BothChannels },
            new object[] { "focus_change", TelemetryStudyChannelMembership.BothChannels },
            new object[] { "value_change", TelemetryStudyChannelMembership.BothChannels },
            new object[] { "manual_step", TelemetryStudyChannelMembership.ObserverOnly },
            new object[] { "ambiguity_resolved", TelemetryStudyChannelMembership.BothChannels },
            new object[] { "stt_error", TelemetryStudyChannelMembership.VoiceOnly },
            new object[] { "intent_recognized", TelemetryStudyChannelMembership.VoiceOnly },
            new object[] { "command_recognition_failed", TelemetryStudyChannelMembership.VoiceOnly },
            new object[] { "no_speech_detected", TelemetryStudyChannelMembership.VoiceOnly },
            new object[] { "system_feedback", TelemetryStudyChannelMembership.VoiceOnly },
            new object[] { "input_mode_changed", TelemetryStudyChannelMembership.VoiceOnly },
            new object[] { "destructive_command_resolved", TelemetryStudyChannelMembership.VoiceOnly },
            new object[] { "barge_in", TelemetryStudyChannelMembership.VoiceOnly },
            new object[] { "conversational_pacing_signal", TelemetryStudyChannelMembership.VoiceOnly },
            new object[] { "wake_word_listener_setup", TelemetryStudyChannelMembership.NonComparableDiagnostic },
            new object[] { "wake_word_acoustic_detection", TelemetryStudyChannelMembership.NonComparableDiagnostic },
            new object[] { "wake_word_capture_loop", TelemetryStudyChannelMembership.NonComparableDiagnostic },
            new object[] { "wake_word_transcription_rejection", TelemetryStudyChannelMembership.NonComparableDiagnostic },
            new object[] { "wake_stitch", TelemetryStudyChannelMembership.NonComparableDiagnostic },
            new object[] { "wake_word_miss", TelemetryStudyChannelMembership.NonComparableDiagnostic },
            new object[] { "wake_word_activation", TelemetryStudyChannelMembership.NonComparableDiagnostic },
            new object[] { "wake_word_fallback", TelemetryStudyChannelMembership.NonComparableDiagnostic },
            new object[] { "language_detected", TelemetryStudyChannelMembership.NonComparableDiagnostic },
        };
}
