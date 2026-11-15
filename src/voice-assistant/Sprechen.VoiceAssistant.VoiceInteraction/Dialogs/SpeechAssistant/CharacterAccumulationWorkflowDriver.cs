using Microsoft.Extensions.Logging;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Steuert das schrittweise Erfassen, Sammeln und Abschließen eines Feldwerts
/// über die jeweilige Eingabestrategie. Bei vorhandenem Inhalt folgt auf die
/// erste als Stille eingestufte Aufnahme eine Nachfrage, auf die zweite solche
/// Aufnahme in Folge ein Abschluss. Bei leerem Puffer wird die Feldhilfe verstärkt und weiter
/// aufgenommen. Erkennungsfehler zählen gesondert und führen nach begrenzten
/// Wiederholungen zur Rückgabe ohne Wert. Befehle können ein Dialogergebnis
/// liefern oder die Eingabe nach Fokus- oder Moduswechsel beenden.
/// </summary>
internal sealed class CharacterAccumulationWorkflowDriver
{
    private readonly ILogger<CharacterAccumulationWorkflowDriver> _logger;

    public CharacterAccumulationWorkflowDriver(ILogger<CharacterAccumulationWorkflowDriver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CharacterAccumulationResult> RunAsync(
        ICharacterAccumulationStrategy strategy,
        CharacterAccumulationContext context,
        string initialTranscript,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(strategy);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(initialTranscript);

        var currentTranscript = initialTranscript.Trim();

        // Die Wiederholungsgrenze verwendet das modellierte MaxRetries auch für
        // Erkennungsfehler. Mindestens eine Wiederholung bleibt erlaubt, selbst
        // wenn der konfigurierte Wert null ist.
        var recognizerFaultBudget = Math.Max(context.Context.VoiceSettings.GetMaxRetries(), 1);
        var consecutiveRecognizerFaults = 0;

        while (true)
        {
            var outcome = await strategy.ProcessTurnAsync(context, currentTranscript, cancellationToken)
                .ConfigureAwait(false);
            // Ein Befehl beendet die Eingabe mit einem Dialogergebnis oder nach Fokus-
            // oder Moduswechsel. Der bisherige Puffer wird dabei nicht übernommen.
            if (outcome.DialogResult is { } dialogResult)
            {
                return CharacterAccumulationResult.CommandExecuted(dialogResult);
            }

            if (outcome.ExitedOnFocusOrModeChange)
            {
                return CharacterAccumulationResult.FocusOrModeChanged();
            }

            if (outcome.Finalized)
            {
                // Eine ausdrückliche Übernahme gesondert weitergeben, damit die Feldbestätigung
                // sie berücksichtigen kann. Der Abschluss nach fehlender Sprache setzt
                // dieses Kennzeichen nicht.
                return outcome.FinalizedByExplicitAccept
                    ? CharacterAccumulationResult.CapturedByExplicitAccept(
                        strategy.GetFinalizedValue(),
                        strategy.AggregatedConfidencePercent)
                    : CharacterAccumulationResult.Captured(
                        strategy.GetFinalizedValue(),
                        strategy.AggregatedConfidencePercent);
            }

            var nextCapture = await strategy.CaptureNextTurnAsync(context, cancellationToken)
                .ConfigureAwait(false);

            // Bei vorhandenem Inhalt nach der ersten als Stille eingestuften Aufnahme
            // nachfragen. Erst die zweite solche Aufnahme in Folge schließt den Wert ab. Bei leerem Puffer
            // die Feldhilfe verstärken und weiter aufnehmen.
            var rePromptedThisSilence = false;
            while (nextCapture.Kind != CharacterAccumulationCaptureKind.Transcribed)
            {
                // Erkennungsfehler zählen nicht als Stille und setzen die Nachfragefolge
                // zurück. Begrenzt wiederholen, danach ohne Wert zurückkehren. Keine
                // Feldhilfe auslösen, da der Fehler keine unverstandene Nutzereingabe belegt.
                if (nextCapture.Kind == CharacterAccumulationCaptureKind.RecognizerUnavailable)
                {
                    if (++consecutiveRecognizerFaults > recognizerFaultBudget)
                    {
                        // Den Neustart ankündigen, damit ein zuvor diktierter Teilwert nicht als
                        // weiterhin vorhanden gilt. Scheitert diese Ansage ebenfalls, trotzdem ohne
                        // Wert zurückkehren. Einen angeforderten Abbruch dagegen weitergeben.
                        try
                        {
                            await strategy.AnnounceRecognizerUnavailableAsync(context, cancellationToken)
                                .ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(
                                ex,
                                "Recognizer-unavailable announcement failed; handing the field back uncaptured anyway.");
                        }

                        return CharacterAccumulationResult.NotCaptured();
                    }

                    rePromptedThisSilence = false;
                    nextCapture = await strategy.CaptureNextTurnAsync(context, cancellationToken)
                        .ConfigureAwait(false);
                    continue;
                }

                if (strategy.HasContent)
                {
                    if (!rePromptedThisSilence)
                    {
                        rePromptedThisSilence = true;
                        await strategy.AnnounceContinueOrDoneAsync(context, cancellationToken)
                            .ConfigureAwait(false);
                        nextCapture = await strategy.CaptureNextTurnAsync(context, cancellationToken)
                            .ConfigureAwait(false);
                        continue;
                    }

                    return CharacterAccumulationResult.Captured(
                        strategy.GetFinalizedValue(),
                        strategy.AggregatedConfidencePercent);
                }

                await context.AutoHelp.RegisterFailureAsync(context.Field.FieldId, cancellationToken)
                    .ConfigureAwait(false);
                nextCapture = await strategy.CaptureNextTurnAsync(context, cancellationToken)
                    .ConfigureAwait(false);
            }

            // Ein geliefertes Transkript setzt den Fehlerzähler zurück. Einzelne
            // Erkennungsfehler zwischen erfolgreichen Aufnahmen verbrauchen dadurch
            // keine gemeinsame Gesamtgrenze.
            consecutiveRecognizerFaults = 0;
            currentTranscript = nextCapture.Transcript!;
        }
    }
}
