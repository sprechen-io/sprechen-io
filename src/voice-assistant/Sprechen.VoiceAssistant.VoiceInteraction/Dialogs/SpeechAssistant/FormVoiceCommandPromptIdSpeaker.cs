using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Löst Ansagekennungen auf, protokolliert die Anforderung mit einer gezielten Sperre für werttragende Diagnosetexte und fordert die Wiedergabe an.
/// </summary>
internal sealed class FormVoiceCommandPromptIdSpeaker
{
    // Für diese Ansagemarker nur die Kennung protokollieren, weil ihre Texte Werte
    // oder geschützte Vorleseinhalte enthalten können. Die Liste ist eine gezielte Textsperre.
    private static readonly string[] ValueBearingPromptIdMarkers =
    [
        "readback",
        "summary.value",
        "summary.stored",
        "validation.invalidvalue",
        "validation.context",
        "digits.current",
        "digits.workflow.captured",
        "digits.redictate",
        "digits.lastdigit",
        "digits.lastblock"
    ];

    private readonly IFormVoiceCommandSpeechService _speechService;
    private readonly FormVoiceCommandPromptTextResolver _promptTextResolver;
    private readonly ISpeechAssistantTrace? _trace;

    public FormVoiceCommandPromptIdSpeaker(
        IFormVoiceCommandSpeechService speechService,
        FormVoiceCommandPromptTextResolver promptTextResolver,
        ISpeechAssistantTrace? trace = null)
    {
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
        _promptTextResolver = promptTextResolver ?? throw new ArgumentNullException(nameof(promptTextResolver));
        _trace = trace;
    }

    public async Task SpeakAsync(
        FormContract contract,
        ImmutableArray<string> promptIds,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        CancellationToken cancellationToken)
    {
        // Eine gemeinsame Zuordnungskennung verbindet die angeforderten Ansagen dieses
        // Aufrufs in der Diagnose.
        var correlationId = _trace is null ? null : Guid.NewGuid().ToString("N");

        foreach (var promptId in promptIds)
        {
            var resolved = _promptTextResolver.Resolve(contract, [promptId], isoCode, voiceSettings);
            foreach (var promptText in resolved)
            {
                await TraceSpokenPromptAsync(correlationId, promptId, promptText, cancellationToken).ConfigureAwait(false);
                await _speechService.SpeakSafeAsync(promptText, isoCode, voiceSettings, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task TraceSpokenPromptAsync(
        string? correlationId,
        string promptId,
        string promptText,
        CancellationToken cancellationToken)
    {
        if (_trace is null)
        {
            return;
        }

        // Die Ansagekennung immer protokollieren. Aufgelösten Text nur hinzufügen, wenn
        // der Marker nicht unter die hier konfigurierte Textsperre fällt.
        var message = MayLogResolvedText(promptId)
            ? $"prompt spoken id={promptId} text={promptText}"
            : $"prompt spoken id={promptId}";

        await _trace
            .WriteIfConfiguredAsync(
                correlationId,
                SpeechAssistantTraceScope.VoiceCommandEffect,
                SpeechAssistantTraceLevel.Debug,
                message,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static bool MayLogResolvedText(string promptId)
    {
        if (promptId.StartsWith("privacy.", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        foreach (var marker in ValueBearingPromptIdMarkers)
        {
            if (promptId.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
