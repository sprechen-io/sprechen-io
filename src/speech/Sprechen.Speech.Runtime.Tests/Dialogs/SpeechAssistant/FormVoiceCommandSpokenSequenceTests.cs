using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.ModelRuntime.PromptTemplates;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Reihenfolge und Wiederholung angeforderter Sprachausgaben des Formulardialogs.
/// Erkennung ist vorgegeben, ein TTS-Double protokolliert die Texte. Fehler zeigen
/// die nummerierte Folge, damit doppelte oder falsch platzierte Rückmeldungen sichtbar werden.
/// </summary>
public sealed class FormVoiceCommandSpokenSequenceTests
{
    [Fact]
    public async Task SpokenSequence_WhenEveryCaptureOnAFieldIsInterrupted_NoTurnIsRecordedAsSilence_AndNoSentenceScalesWithTheFaults()
    {
        // Sechs behebbaren Aufnahmefehlern müssen technische Rückmeldungen zugeordnet werden,
        // ohne sie als fehlende Teilnehmereingabe zu zählen oder Zusatzhinweise je Fehler zu wiederholen.
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new ScriptedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(FormVoiceCommandDialogTests.CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(FormVoiceCommandDialogTests.CreateInputModeContract(InputModes.Voice));
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(FormVoiceCommandDialogTests.CreateTelemetry(
            telemetryLogger));

        var fault = FormVoiceCommandDialogTests.CreateRecoverableCaptureFaultStep("stream-stalled");

        const int FaultedTurns = 6;
        for (var i = 0; i < FaultedTurns; i++)
        {
            await speechClient.EnqueueRecordStepAsync(fault);
        }

        // Die Feldfrage darf vor jeder Aufnahme erneut erscheinen; alle anderen Ansagen
        // müssen unter der Zahl der eingespeisten Fehler bleiben. Kein Fehler zählt als fehlende Sprache.
        for (var i = 0; i < 4; i++)
        {
            await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(
                new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        }

        speechClient.EnqueueTranscript("Markus");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var spoken = await RunAndCaptureSpokenSequenceAsync(
            messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry, "six interrupted captures");

        var silentTurns = telemetry.Calls.Count(call => call.Method == "TrackNoSpeechDetected");
        Assert.True(
            silentTurns == 0,
            $"{silentTurns} of {FaultedTurns} interrupted captures were recorded as the participant "
            + $"saying nothing.{Environment.NewLine}{Describe(spoken)}");

        var fieldPromptFragment = "Feld 1 von 1";
        foreach (var group in spoken
                     .Where(line => !line.Contains(fieldPromptFragment, StringComparison.Ordinal))
                     .GroupBy(line => line, StringComparer.Ordinal))
        {
            Assert.True(
                group.Count() < FaultedTurns,
                $"'{Shorten(group.Key)}' was spoken {group.Count()} times across {FaultedTurns} faulted turns - "
                + "it scales with the fault count, so a participant on a broken recognizer hears it "
                + $"without end.{Environment.NewLine}{Describe(spoken)}");
        }
    }

    // Die automatische Wiederholung wird in diesen Szenarien nicht geprüft.

    [Fact]
    public async Task SpokenSequence_WhenTheRecognizerFails_TheParticipantIsNeverToldTheyWereNotUnderstood()
    {
        // Ein technischer Aufnahmefehler verlangt eine technische Erklärung, keine Aufforderung, eine angeblich unverständliche Äußerung umzuformulieren.
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new ScriptedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(FormVoiceCommandDialogTests.CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(FormVoiceCommandDialogTests.CreateInputModeContract(InputModes.Voice));
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(FormVoiceCommandDialogTests.CreateTelemetry(
            telemetryLogger));

        var fault = FormVoiceCommandDialogTests.CreateRecoverableCaptureFaultStep("stream-stalled");
        for (var i = 0; i < 4; i++)
        {
            await speechClient.EnqueueRecordStepAsync(fault);
        }

        for (var i = 0; i < 4; i++)
        {
            await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(
                new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        }

        speechClient.EnqueueTranscript("Markus");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var spoken = await RunAndCaptureSpokenSequenceAsync(
            messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry, "four mic-gate faults");

        var notUnderstood = RecoverableCapturePromptTextResolver.GetRecoverableFailurePrompt("de", PolitenessLevel.Formal);
        var offending = spoken.Where(line => line.Contains(notUnderstood, StringComparison.Ordinal)).ToArray();

        Assert.True(
            offending.Length == 0,
            $"The dialog told the participant it could not understand them, on a turn where the "
            + $"recognition never happened.{Environment.NewLine}{Describe(spoken)}");
    }


    [Fact]
    public async Task SpokenSequence_TwoUnrecognizedUtterancesInARow_ProduceTwoDifferentPrompts()
    {
        // Nach wiederholt unbekanntem Kommando muss eine zusätzliche Hilfemöglichkeit erscheinen.
        // Geprüft werden die im Dialog zusammengesetzten Texte und ihre Reihenfolge.
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new ScriptedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(FormVoiceCommandDialogTests.CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(FormVoiceCommandDialogTests.CreateInputModeContract(InputModes.Voice));
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(FormVoiceCommandDialogTests.CreateTelemetry(
            telemetryLogger));

        for (var i = 0; i < 6; i++)
        {
            await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(
                new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        }

        speechClient.EnqueueTranscript("befehl unsinn eins");
        speechClient.EnqueueTranscript("befehl unsinn zwei");
        speechClient.EnqueueTranscript("Markus");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var spoken = await RunAndCaptureSpokenSequenceAsync(
            messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry, "two unrecognized utterances");

        var responses = spoken
            .Where(line => line.Contains("unsinn eins", StringComparison.Ordinal)
                || line.Contains("unsinn zwei", StringComparison.Ordinal))
            .ToArray();

        Assert.True(
            responses.Length == 2,
            $"Expected one spoken response per unrecognized utterance, got {responses.Length}. Both must "
            + $"be answered, and each must echo what was heard so the participant can tell a misheard "
            + $"word from a rejected one.{Environment.NewLine}{Describe(spoken)}");

        // Das zitierte Transkript vor dem Vergleich entfernen: Andere Eingabewörter allein ergeben noch keine neue Hilfestellung.
        var first = responses[0].Replace("befehl unsinn eins", string.Empty, StringComparison.Ordinal);
        var second = responses[1].Replace("befehl unsinn zwei", string.Empty, StringComparison.Ordinal);

        Assert.True(
            !string.Equals(first, second, StringComparison.Ordinal),
            $"Both unrecognized utterances were answered with the same sentence, so the second turn "
            + $"taught nothing: '{Shorten(first)}'.{Environment.NewLine}{Describe(spoken)}");

        Assert.True(
            second.Contains("Hilfe", StringComparison.OrdinalIgnoreCase),
            $"The second unrecognized utterance did not name the way out. A participant who has now "
            + $"failed twice needs an affordance, not another apology: '{Shorten(second)}'."
            + $"{Environment.NewLine}{Describe(spoken)}");
    }

    [Fact]
    public async Task SpokenSequence_WhenBackCannotMove_TheParticipantHearsWhyOnceBeforeTheFieldReturns()
    {
        // Am ersten Feld ist Zurück nicht möglich. Die Erklärung muss vor der erneuten Feldfrage folgen, damit sie dem ausgeführten Kommando zugeordnet werden kann.
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new ScriptedSpeechInputClient();
        var tts = new RecordingTextToSpeechPlayer();
        var mdaCatalog = new FixedFormsCatalogDataClient(FormVoiceCommandDialogTests.CreateOneFieldDefinitions());
        var mdaFormContracts = new FixedMdaFormContractDataClient(FormVoiceCommandDialogTests.CreateInputModeContract(InputModes.Voice));
        var telemetryLogger = new StructuredCapturingLogger<EvaluationTelemetry>();
        var telemetry = new RecordingEvaluationTelemetry(FormVoiceCommandDialogTests.CreateTelemetry(
            telemetryLogger));

        for (var i = 0; i < 6; i++)
        {
            await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(
                new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, new[] { new byte[] { 1, 2 } })));
        }

        // Im Textfeld markiert der Aktivierungswort-Anker Zurück als Kommando; ohne Anker wäre es ein Feldwert.
        speechClient.EnqueueTranscript("okay alex zurück");
        speechClient.EnqueueTranscript("Markus");
        speechClient.EnqueueTranscript("übernehmen");
        speechClient.EnqueueTranscript("absenden");

        var spoken = await RunAndCaptureSpokenSequenceAsync(
            messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry, "back at the first field");

        Assert.True(
            VoiceInterfaceModelCatalog.Resources.TryGetPromptTemplate(FormDialogPromptIds.BackUnavailable, "de", out var expected),
            "The generic prompt catalog carries no German text for the back-unavailable prompt id.");

        var positions = spoken
            .Select((line, index) => (line, index))
            .Where(entry => entry.line.Contains(expected, StringComparison.Ordinal))
            .Select(entry => entry.index)
            .ToArray();

        Assert.True(
            positions.Length == 1,
            $"The failed Back command was answered {positions.Length} times instead of once."
            + $"{Environment.NewLine}{Describe(spoken)}");

        var fieldPromptAfterwards = spoken
            .Skip(positions[0] + 1)
            .Any(line => line.Contains("Feld 1 von 1", StringComparison.Ordinal));

        Assert.True(
            fieldPromptAfterwards,
            "After the availability answer the participant was never returned to their field, so the "
            + $"turn ended on the refusal.{Environment.NewLine}{Describe(spoken)}");
    }

    // Die Assertions prüfen TTS-Aufrufreihenfolge, nicht Mikrofonaktivierung oder hörbare Hinweistöne.

    private static async Task<IReadOnlyList<string>> RunAndCaptureSpokenSequenceAsync(
        SynchronousClientMessageBus messageBus,
        ScriptedSpeechInputClient speechClient,
        RecordingTextToSpeechPlayer tts,
        FixedFormsCatalogDataClient mdaCatalog,
        FixedMdaFormContractDataClient mdaFormContracts,
        RecordingEvaluationTelemetry telemetry,
        string scenario)
    {
        var dialog = FormVoiceCommandDialogTests.CreateDialog(messageBus, speechClient, tts, mdaCatalog, mdaFormContracts, telemetry);

        var executionTask = dialog.ExecuteAsync(
            "de",
            "one-field",
            formId: null,
            SpeechAssistantApplicationSessionState.Empty,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
            CancellationToken.None);

        var completed = await Task.WhenAny(executionTask, Task.Delay(FormVoiceCommandDialogTests.DialogExecutionTimeout));
        var spoken = tts.Calls.Select(call => call.Text).ToArray();

        if (!ReferenceEquals(executionTask, completed))
        {
            // Bei Zeitüberschreitung zeigt die bisher erfasste TTS-Folge, wo der Ablauf stehen blieb.
            throw new Xunit.Sdk.XunitException(
                $"The dialog did not finish ({scenario}). RecordCallCount={speechClient.RecordCallCount}."
                + $"{Environment.NewLine}{Describe(spoken)}");
        }

        await executionTask;
        return spoken;
    }

    private static string Describe(IReadOnlyList<string> spoken)
        => "What the participant heard, in order:" + Environment.NewLine
            + string.Join(
                Environment.NewLine,
                spoken.Select((line, index) => $"  {index + 1,2}. {Shorten(line)}"));

    private static string Shorten(string line)
        => line.Length <= 110 ? line : line[..107] + "...";
}
