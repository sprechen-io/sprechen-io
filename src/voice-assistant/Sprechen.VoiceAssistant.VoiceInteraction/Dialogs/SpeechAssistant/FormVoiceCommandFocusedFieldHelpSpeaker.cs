using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Fordert Feldposition, Hilfe, Beispiele und Optionen in geordneter Folge an und begrenzt wiederkehrende Befehlshinweise über feldbezogene Merker.
/// </summary>
internal sealed class FormVoiceCommandFocusedFieldHelpSpeaker
{
    private readonly IFormVoiceCommandSpeechService _speechService;

    public FormVoiceCommandFocusedFieldHelpSpeaker(IFormVoiceCommandSpeechService speechService)
    {
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    public async Task SpeakAsync(
        IFormDialogRuntime interpreter,
        IFormVoiceCommandDialogStateTracker stateTracker,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(interpreter);
        ArgumentNullException.ThrowIfNull(stateTracker);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentNullException.ThrowIfNull(voiceSettings);

        var field = await interpreter.GetFocusedFieldAsync().ConfigureAwait(false);

        // Eine nichtleere Feldkennung für den feldbezogenen Hinweismerker verlangen.
        if (string.IsNullOrWhiteSpace(field.FieldId))
        {
            throw new InvalidOperationException("The focused field snapshot carries no FieldId; the per-field help latch cannot be keyed.");
        }

        // Zuerst dieselbe Positionszeile wie beim Feldeinstieg anfordern.
        // Danach folgen Hilfetext, Beispiele, Optionen und die Befehlshinweise.
        var statusSnapshot = await interpreter.GetStatusSnapshotAsync().ConfigureAwait(false);
        var openingLine = FormFieldPositionPromptBuilder.GetFieldPositionPrompt(
            interpreter.Contract,
            isoCode,
            interpreter.PolitenessLevel,
            statusSnapshot.CurrentFieldIndex,
            statusSnapshot.Progress.AllFields.Total,
            field.Label,
            expectedInputHint: null);

        if (!string.IsNullOrWhiteSpace(openingLine))
        {
            await _speechService.SpeakSafeAsync(openingLine, isoCode, voiceSettings, cancellationToken).ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(field.HelpText))
        {
            await _speechService.SpeakSafeAsync(field.HelpText, isoCode, voiceSettings, cancellationToken).ConfigureAwait(false);
        }

        foreach (var example in field.Examples)
        {
            if (string.IsNullOrWhiteSpace(example))
            {
                continue;
            }

            await _speechService.SpeakSafeAsync(example, isoCode, voiceSettings, cancellationToken).ConfigureAwait(false);
        }

        if (field.Options.Length > 0)
        {
            var items = field.Options
                .Select((option, index) => $"{index + 1}: {option.DisplayName}")
                .ToArray();

            await _speechService.SpeakSafeAsync(
                    FormOptionsPromptBuilder.FormatOptionsPrompt(interpreter.Contract, isoCode, interpreter.PolitenessLevel, items),
                    isoCode,
                    voiceSettings,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        // Nur bei vorhandenem Verweistext den feldbezogenen Merker belegen und
        // den Hinweis zur Befehlsliste anfordern. Der Merker wird vor der Ausgabe gesetzt.
        var commandsPointer = FormHelpCommandsPointerPromptBuilder.BuildPrompt(
            interpreter.Contract,
            isoCode,
            interpreter.PolitenessLevel);
        if (!string.IsNullOrWhiteSpace(commandsPointer)
            && !stateTracker.HasAlreadyTaught($"{DialogTeachOnceTopics.HelpCommandsPointer}:{field.FieldId}"))
        {
            await _speechService.SpeakSafeAsync(commandsPointer, isoCode, voiceSettings, cancellationToken).ConfigureAwait(false);
        }

        // Das akzeptierte Aktivierungswort als Befehlshinweis nennen.
        // Ein Kennzeichen je Feld begrenzt die Wiederholung dieses Hinweises.
        var commandAnchorHint = FormCommandAnchorHintPromptBuilder.BuildPrompt(
            interpreter.Contract,
            isoCode,
            interpreter.PolitenessLevel);
        if (!string.IsNullOrWhiteSpace(commandAnchorHint)
            && !stateTracker.HasAlreadyTaught($"{DialogTeachOnceTopics.HelpCommandAnchor}:{field.FieldId}"))
        {
            await _speechService.SpeakSafeAsync(commandAnchorHint, isoCode, voiceSettings, cancellationToken).ConfigureAwait(false);
        }
    }
}
