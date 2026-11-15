using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Speech.App.Runtime.Services;

/// <summary>
/// Verknüpft aufeinanderfolgende Erkennungsabschnitte für die Aktivierung und bewahrt
/// dafür einen begonnenen Abschnitt. Führt keine Ein- oder Ausgabe aus. Die Aufnahmezeiten
/// liefert der Aufrufer als DateTimeOffset.
/// </summary>
public sealed class WakeUtteranceStitcher
{
    private WakeStitcherCarriedCycle? _carried;

    public void Reset() => _carried = null;

    public string? CarriedTranscriptText => _carried?.TranscriptText;

    /// <summary>
    /// Verweist auf den Beginn des bewahrten Abschnitts im <see cref="SpeechSampleRing"/>, damit Zusammenführung und Diagnose dieselbe Audioherkunft verwenden.
    /// Ohne bewahrten Abschnitt ist der Wert <c>null</c>. Ein zweiter Audioschnappschuss wird nicht gehalten.
    /// </summary>
    public SampleCursor? CarriedCursor => _carried?.Cursor;

    public WakeStitchResult AttachOrStart(
        SpeechTranscript transcript,
        DateTimeOffset capturedAt,
        WakeWordInventory inventory,
        WakeUtteranceWindow? window)
        => AttachOrStart(transcript, capturedAt, inventory, window, cursor: default);

    public WakeStitchResult AttachOrStart(
        SpeechTranscript transcript,
        DateTimeOffset capturedAt,
        WakeWordInventory inventory,
        WakeUtteranceWindow? window,
        SampleCursor cursor)
    {
        ArgumentNullException.ThrowIfNull(transcript);
        ArgumentNullException.ThrowIfNull(inventory);

        if (window is null || window.MaxCarriedCycles < 1)
        {
            return WakeStitchNoOpResult.Instance;
        }

        var transcriptText = transcript.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(transcriptText))
        {
            return _carried is null
                ? WakeStitchNoOpResult.Instance
                : EvaluateExpiry(capturedAt, window);
        }

        if (_carried is { } carried)
        {
            var pauseMs = (capturedAt - carried.CapturedAt).TotalMilliseconds;
            if (pauseMs < 0 || pauseMs > window.MaxInterCyclePauseMs)
            {
                _carried = null;

                // Trotz abgelaufenen Zeitfensters kann der aktuelle Abschnitt einen
                // neuen Aktivierungsversuch beginnen. Nur wenn er dafür ungeeignet
                // ist, wird der Ablauf des vorherigen Abschnitts gemeldet.
                return TryStartCarry(transcriptText, capturedAt, inventory, window, cursor)
                    ?? new WakeStitchExpiredResult(carried.TranscriptText);
            }

            var stitchedText = string.Concat(carried.TranscriptText, " ", transcriptText);
            var evaluation = WakeWordActivationRule.EvaluateLeadingWakeWord(stitchedText, transcript.ConfidencePercent, inventory);
            _carried = null;
            if (evaluation.Status == WakeWordActivationStatus.LeadingWakeWordDetected)
            {
                return new WakeStitchAttachedResult(stitchedText, evaluation.CommandText ?? string.Empty, evaluation);
            }

            // Nach erfolgloser Zusammenführung kann der aktuelle Abschnitt einen
            // neuen Aktivierungsversuch beginnen und darf deshalb erhalten bleiben.
            return TryStartCarry(transcriptText, capturedAt, inventory, window, cursor)
                ?? WakeStitchNoOpResult.Instance;
        }

        return TryStartCarry(transcriptText, capturedAt, inventory, window, cursor)
            ?? WakeStitchNoOpResult.Instance;
    }

    private WakeStitchResult? TryStartCarry(
        string transcriptText,
        DateTimeOffset capturedAt,
        WakeWordInventory inventory,
        WakeUtteranceWindow window,
        SampleCursor cursor)
    {
        if (window.PrefixOnlyFirstCycleAccepted && !IsStrictPrefixOfAnyActivationForm(transcriptText, inventory))
        {
            return null;
        }

        _carried = new WakeStitcherCarriedCycle(transcriptText, capturedAt, cursor);
        return new WakeStitchStartedResult(transcriptText);
    }

    private WakeStitchResult EvaluateExpiry(DateTimeOffset capturedAt, WakeUtteranceWindow window)
    {
        var carried = _carried!;
        var pauseMs = (capturedAt - carried.CapturedAt).TotalMilliseconds;
        if (pauseMs < 0 || pauseMs > window.MaxInterCyclePauseMs)
        {
            _carried = null;
            return new WakeStitchExpiredResult(carried.TranscriptText);
        }

        return WakeStitchNoOpResult.Instance;
    }

    private static bool IsStrictPrefixOfAnyActivationForm(string transcriptText, WakeWordInventory inventory)
    {
        var transcriptTokens = TokenizeForPrefix(transcriptText);
        if (transcriptTokens.Length == 0)
        {
            return false;
        }

        foreach (var form in inventory.ActivationForms.Concat(inventory.RecognitionOnlyActivationForms))
        {
            var formTokens = TokenizeForPrefix(form);
            if (formTokens.Length <= transcriptTokens.Length)
            {
                continue;
            }

            var prefixMatch = true;
            for (var index = 0; index < transcriptTokens.Length; index++)
            {
                if (!string.Equals(transcriptTokens[index], formTokens[index], StringComparison.Ordinal))
                {
                    prefixMatch = false;
                    break;
                }
            }

            if (prefixMatch)
            {
                return true;
            }
        }

        return false;
    }

    private static string[] TokenizeForPrefix(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        var normalized = TextNormalizer.NormalizeForTokenComparison(value);
        return TextNormalizer.TokenizeLettersOrDigits(normalized).ToArray();
    }
}
