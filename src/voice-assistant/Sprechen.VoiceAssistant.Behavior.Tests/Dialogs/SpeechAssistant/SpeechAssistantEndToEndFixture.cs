using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.VoiceAssistant.Behavior.Tests.TestDoubles;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

internal sealed class SpeechAssistantEndToEndFixture
{
    private readonly SpeechAssistantCatalogBuilder catalogBuilder = new();
    private readonly SpeechAssistantDialogStateDescriber dialogStateDescriber = new();

    internal (RecordingTextToSpeechPlayer TextToSpeechPlayer, RecordingEarconPlayer EarconPlayer, Func<IReadOnlyList<string>> ReadFeedback)
        CreateFeedbackRecorders()
    {
        var gate = new object();
        var feedback = new List<string>();

        void Record(string entry)
        {
            lock (gate)
            {
                feedback.Add(entry);
            }
        }

        IReadOnlyList<string> ReadFeedback()
        {
            lock (gate)
            {
                return feedback.ToArray();
            }
        }

        return (new RecordingTextToSpeechPlayer(Record), new RecordingEarconPlayer(Record), ReadFeedback);
    }

    internal int GetFeedbackCursor(Func<IReadOnlyList<string>> readFeedback)
    {
        ArgumentNullException.ThrowIfNull(readFeedback);

        return readFeedback().Count;
    }

    internal async Task AssertFeedbackProducedAfterAsync(
        Func<IReadOnlyList<string>> readFeedback,
        int cursor,
        TimeSpan timeout,
        string windowName)
    {
        ArgumentNullException.ThrowIfNull(readFeedback);
        ArgumentException.ThrowIfNullOrWhiteSpace(windowName);

        await WaitUntilAsync(() => readFeedback().Count > cursor, timeout);

        var feedback = readFeedback();
        Assert.True(
            feedback.Count > cursor,
            $"Expected TTS or earcon feedback after {windowName}. Recorded feedback: {string.Join(" | ", feedback)}");
    }

    /// <summary>
    /// Bereitet eine Aufnahme und den Namen der eingestellten Sprache als Zustimmung zum ersten Sprachangebot vor.
    /// Beim Start von SpeechAssistantService läuft diese Entscheidung vor dem Aktivierungsdialog.
    /// Die Antwort stammt aus dem aktuellen Sprachzustand, damit keine weiteren unbeantworteten Angebote entstehen.
    /// Bei direkt gestarteten Dialogtests entfällt dieser Schritt; eine zusätzliche Eingabe würde dort den Ablauf verschieben.
    /// </summary>
    internal async Task EnqueueKeptSessionEntryLanguageAsync(
        QueuedSpeechInputClient client,
        UserLanguageState userLanguageState)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(userLanguageState);

        var isoCode = userLanguageState.SelectedIsoCode;
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        await client.EnqueueRecordingAsync(CreateUtterance());
        client.EnqueueTranscript(
            VoiceInterfaceLanguageCatalog.CreateDefault().GetEndonym(LanguageCode.ParseIso6391(isoCode)));
    }

    internal async Task EnqueueUtterancesAsync(QueuedSpeechInputClient client, int count)
    {
        ArgumentNullException.ThrowIfNull(client);

        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count must be positive.");
        }

        for (var i = 0; i < count; i++)
        {
            await client.EnqueueRecordingAsync(CreateUtterance());
        }
    }

    internal async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(condition);

        using var cts = new CancellationTokenSource(timeout);

        while (!condition())
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10), cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                throw new TimeoutException("Condition was not met before the timeout.");
            }
        }
    }

    internal TimeoutException CreateLanguageSelectionTimeout(
        SpeechAssistantService assistant,
        UserLanguageState userLanguageState,
        QueuedSpeechInputClient speechInputClient,
        RecordingTextToSpeechPlayer textToSpeechPlayer,
        RecordingLogger<ClientMessageBus> messageBusLogger,
        RecordingLogger<SpeechAssistantService> assistantLogger,
        Exception inner)
    {
        ArgumentNullException.ThrowIfNull(assistant);
        ArgumentNullException.ThrowIfNull(userLanguageState);
        ArgumentNullException.ThrowIfNull(speechInputClient);
        ArgumentNullException.ThrowIfNull(textToSpeechPlayer);
        ArgumentNullException.ThrowIfNull(messageBusLogger);
        ArgumentNullException.ThrowIfNull(assistantLogger);
        ArgumentNullException.ThrowIfNull(inner);

        var ttsDump = string.Join(
            " | ",
            textToSpeechPlayer.Calls.Select(call => $"{call.LanguageCode}:{call.Text}"));

        var sttDump = string.Join(
            " | ",
            speechInputClient.SpeechToTextCalls.Select(call => $"{call.LanguageCode}:{call.Text}"));

        var busDump = string.Join(
            " | ",
            messageBusLogger.Entries.Select(entry => $"{entry.Level}:{entry.Message}:{entry.Exception?.Message}"));

        var assistantDump = string.Join(
            " | ",
            assistantLogger.Entries.Select(entry => $"{entry.Level}:{entry.Message}:{entry.Exception?.Message}"));

        var assistantState = dialogStateDescriber.Describe(assistant);

        return new TimeoutException(
            $"Expected language selection did not complete in time. selectedIso={userLanguageState.SelectedIsoCode ?? "<null>"}, recordCalls={speechInputClient.RecordCallCount}, assistantState={assistantState}, TTS calls: {ttsDump}, STT calls: {sttDump}, busLogs: {busDump}, assistantLogs: {assistantDump}",
            inner);
    }

    internal FormsCatalog CreateMultiFormCatalog(
        string applicationId,
        string defaultLanguage,
        VoiceDialogSettings voiceDialogSettings,
        params (string FormId, string DisplayName)[] forms)
        => catalogBuilder.CreateMultiFormCatalog(applicationId, defaultLanguage, voiceDialogSettings, forms);

    private CapturedSpeechAudio CreateUtterance()
        => new(SpeechAudioMimeType.Pcm, [new byte[] { 1 }]);
}


