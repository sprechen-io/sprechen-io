using System.Buffers.Binary;
using System.Linq;
using Sprechen.Proto;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class PcmSpeechCaptureSegmenterTests
{
    private const int SampleRateHz = 16000;
    private const int BytesPerSample = 2;
    private const short SpeechSampleValue = 1000;
    private static readonly PcmSpeechCaptureSegmenterOptions SegmenterOptions = new();

    [Fact]
    public void ProcessChunk_WhenSpeechStartIsConfirmed_SignalsSpeechStartOnce()
    {
        var segmenter = new PcmSpeechCaptureSegmenter(SpeechAudioMimeType.Pcm, silenceDebounceMs: 3000, SegmenterOptions);

        segmenter.ProcessChunk(CreateSilenceChunkMs(20));

        PcmSpeechCaptureSegmenterUpdate first = default;
        for (var i = 0; i < 6; i++)
        {
            first = segmenter.ProcessChunk(CreateSpeechChunkMs(20));
        }

        Assert.True(first.SpeechStarted);
        Assert.Null(first.CompletedUtterance);
        Assert.Null(first.CompletionException);

        var second = segmenter.ProcessChunk(CreateSpeechChunkMs(20));

        Assert.False(second.SpeechStarted);
        Assert.Null(second.CompletedUtterance);
        Assert.Null(second.CompletionException);
    }

    [Fact]
    public void ProcessChunk_WhenSpeechEndsBeforeConfirmation_ResetsConservesAudioAndWaitsForRealSpeech()
    {
        var segmenter = new PcmSpeechCaptureSegmenter(SpeechAudioMimeType.Pcm, silenceDebounceMs: 200, SegmenterOptions);

        // Die Referenzen der kurzen Kandidaten aufbewahren, um ihren Erhalt nach dem Zurücksetzen zu prüfen.
        var firstCandidate = CreateSpeechChunkMs(20);
        var secondCandidate = CreateSpeechChunkMs(20);
        var first = segmenter.ProcessChunk(firstCandidate);
        var second = segmenter.ProcessChunk(secondCandidate);

        // Ein stiller Block unterbricht den noch nicht bestätigten Sprachbeginn.
        var reset = segmenter.ProcessChunk(CreateSilenceChunkMs(20));

        Assert.False(first.SpeechStarted);
        Assert.False(second.SpeechStarted);
        Assert.False(reset.SpeechStarted);
        Assert.Null(reset.CompletedUtterance);
        Assert.Null(reset.CompletionException);

        PcmSpeechCaptureSegmenterUpdate confirmed = default;
        for (var i = 0; i < 6; i++)
        {
            confirmed = segmenter.ProcessChunk(CreateSpeechChunkMs(20));
        }

        Assert.True(confirmed.SpeechStarted);
        Assert.Null(confirmed.CompletedUtterance);
        Assert.Null(confirmed.CompletionException);

        PcmSpeechCaptureSegmenterUpdate completed = default;
        for (var i = 0; i < 200; i++)
        {
            completed = segmenter.ProcessChunk(CreateSilenceChunkMs(20));
            if (completed.CompletedUtterance is not null)
            {
                break;
            }
        }

        Assert.NotNull(completed.CompletedUtterance);

        Assert.Contains(completed.CompletedUtterance!.Chunks, chunk => ReferenceEquals(chunk, firstCandidate));
        Assert.Contains(completed.CompletedUtterance!.Chunks, chunk => ReferenceEquals(chunk, secondCandidate));
    }

    [Fact]
    public void ProcessChunk_WhenManyFalseStartsOccur_ConservedAudioStaysBoundedByPreRoll()
    {
        var segmenter = new PcmSpeechCaptureSegmenter(SpeechAudioMimeType.Pcm, silenceDebounceMs: 200, SegmenterOptions);

        // Wiederholte kurze Kandidaten dürfen den gepufferten Vorlauf nicht unbegrenzt wachsen lassen.
        for (var cycle = 0; cycle < 50; cycle++)
        {
            segmenter.ProcessChunk(CreateSpeechChunkMs(20));
            segmenter.ProcessChunk(CreateSpeechChunkMs(20));
            segmenter.ProcessChunk(CreateSilenceChunkMs(20));
        }

        for (var i = 0; i < 10; i++)
        {
            segmenter.ProcessChunk(CreateSpeechChunkMs(20));
        }

        PcmSpeechCaptureSegmenterUpdate completed = default;
        for (var i = 0; i < 200; i++)
        {
            completed = segmenter.ProcessChunk(CreateSilenceChunkMs(20));
            if (completed.CompletedUtterance is not null)
            {
                break;
            }
        }

        Assert.NotNull(completed.CompletedUtterance);

        // Die Obergrenze berücksichtigt Vorlauf, zehn Sprachblöcke und den gekürzten Nachlauf.
        Assert.True(
            completed.CompletedUtterance!.Chunks.Count <= SegmenterOptions.MaxPreRollChunks + 10 + 11,
            $"Utterance grew to {completed.CompletedUtterance.Chunks.Count} chunks, beyond the pre-roll-bounded maximum.");
    }

    [Fact]
    public void ProcessChunk_WhenPauseInterruptsSpeech_MaxUtteranceBudgetCountsSpeechOnly()
    {
        var options = new PcmSpeechCaptureSegmenterOptions
        {
            SpeechStartConfirmation = TimeSpan.FromMilliseconds(40),
            TailSilence = TimeSpan.FromMilliseconds(20),
            MaxUtteranceDuration = TimeSpan.FromMilliseconds(100),
            MaxTotalSegmentDuration = TimeSpan.FromSeconds(5),
            MaxInitialSilence = TimeSpan.FromSeconds(30),
        };
        var segmenter = new PcmSpeechCaptureSegmenter(SpeechAudioMimeType.Pcm, silenceDebounceMs: 200, options);

        // Unterschiedliche Amplituden machen den zweiten Sprachabschnitt im Ergebnis erkennbar.
        for (var i = 0; i < 3; i++)
        {
            segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 1000));
        }

        // Die Pause darf das Sprachzeitbudget nicht verbrauchen; ihr Ende liegt vor der Stillegrenze.
        for (var i = 0; i < 5; i++)
        {
            var pauseUpdate = segmenter.ProcessChunk(CreateSilenceChunkMs(20));
            Assert.Null(pauseUpdate.CompletedUtterance);
        }

        PcmSpeechCaptureSegmenterUpdate update = default;
        for (var i = 0; i < 5; i++)
        {
            update = segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 1500));
            if (update.CompletedUtterance is not null)
            {
                break;
            }
        }

        Assert.NotNull(update.CompletedUtterance);
        Assert.Contains(
            update.CompletedUtterance!.Chunks,
            chunk => BinaryPrimitives.ReadInt16LittleEndian(chunk.AsSpan(0, BytesPerSample)) == 1500);
    }

    [Fact]
    public void ProcessChunk_WhenAmplitudeAlternates_ClosesAtTotalSegmentCapNotSpeechBudget()
    {
        var options = new PcmSpeechCaptureSegmenterOptions
        {
            SpeechStartConfirmation = TimeSpan.FromMilliseconds(40),
            TailSilence = TimeSpan.FromMilliseconds(20),
            MaxUtteranceDuration = TimeSpan.FromSeconds(5),
            MaxTotalSegmentDuration = TimeSpan.FromMilliseconds(400),
            MaxInitialSilence = TimeSpan.FromSeconds(30),
        };
        var segmenter = new PcmSpeechCaptureSegmenter(SpeechAudioMimeType.Pcm, silenceDebounceMs: 200, options);

        for (var i = 0; i < 3; i++)
        {
            segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 1000));
        }

        // Kurze Pausen verhindern den Stilleabschluss; hier soll die Grenze für die Gesamtdauer greifen.
        PcmSpeechCaptureSegmenterUpdate update = default;
        var cycles = 0;
        for (; cycles < 200; cycles++)
        {
            segmenter.ProcessChunk(CreateSilenceChunkMs(20));
            update = segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 1000));
            if (update.CompletedUtterance is not null)
            {
                break;
            }
        }

        Assert.NotNull(update.CompletedUtterance);
        Assert.True(cycles < 30, $"Expected closure near the 400ms total cap, but ran {cycles} cycles.");
    }

    [Fact]
    public void ProcessChunk_WhenSpeechEndsAndSilenceExceedsDebounce_CompletesWithTrimmedTail()
    {
        const int debounceMs = 3000;
        var segmenter = new PcmSpeechCaptureSegmenter(SpeechAudioMimeType.Pcm, silenceDebounceMs: debounceMs, SegmenterOptions);

        for (var i = 0; i < 10; i++)
        {
            segmenter.ProcessChunk(CreateSilenceChunkMs(20));
        }

        for (var i = 0; i < 10; i++)
        {
            segmenter.ProcessChunk(CreateSpeechChunkMs(20));
        }

        var completed = false;
        var lastUpdate = default(PcmSpeechCaptureSegmenterUpdate);

        for (var i = 0; i < 150; i++)
        {
            lastUpdate = segmenter.ProcessChunk(CreateSilenceChunkMs(20));
            if (lastUpdate.CompletedUtterance is not null)
            {
                completed = true;
                break;
            }
        }

        Assert.True(completed);
        Assert.NotNull(lastUpdate.CompletedUtterance);
        Assert.Null(lastUpdate.CompletionException);

        // Erwartet werden zehn Vorlauf-, zehn Sprach- und 25 Nachlaufblöcke.
        Assert.Equal(45, lastUpdate.CompletedUtterance!.Chunks.Count);
    }

    [Fact]
    public void ProcessChunk_WhenTrailingConsonantDecaysBelowThreshold_RetainsItPastTheOldTailCap()
    {
        // Auch unterhalb der Sprachschwelle sollen Daten im Nachlauf erhalten bleiben.
        const int debounceMs = 3000;
        var segmenter = new PcmSpeechCaptureSegmenter(SpeechAudioMimeType.Pcm, silenceDebounceMs: debounceMs, SegmenterOptions);

        for (var i = 0; i < 10; i++)
        {
            segmenter.ProcessChunk(CreateSpeechChunkMs(20));
        }

        // Den Prüfblock nach 280 ms Stille platzieren.
        for (var i = 0; i < 14; i++)
        {
            segmenter.ProcessChunk(CreateSilenceChunkMs(20));
        }

        // Der zusätzliche stille Block dient als per Referenz identifizierbarer Marker im Nachlauf.
        var trailingConsonant = CreateSilenceChunkMs(20);
        segmenter.ProcessChunk(trailingConsonant);

        PcmSpeechCaptureSegmenterUpdate update = default;
        for (var i = 0; i < 200; i++)
        {
            update = segmenter.ProcessChunk(CreateSilenceChunkMs(20));
            if (update.CompletedUtterance is not null)
            {
                break;
            }
        }

        Assert.NotNull(update.CompletedUtterance);
        Assert.Contains(
            update.CompletedUtterance!.Chunks,
            chunk => ReferenceEquals(chunk, trailingConsonant));
    }

    [Fact]
    public void ProcessChunk_WhenSpeechStarts_DoesNotDuplicateFirstSpeechChunkAndKeepsBufferedPreRoll()
    {
        const int debounceMs = 3000;
        var segmenter = new PcmSpeechCaptureSegmenter(SpeechAudioMimeType.Pcm, silenceDebounceMs: debounceMs, SegmenterOptions);

        var silenceChunks = new List<byte[]>(capacity: 10);
        for (var i = 0; i < 10; i++)
        {
            var chunk = CreateSilenceChunkMs(20);
            silenceChunks.Add(chunk);
            segmenter.ProcessChunk(chunk);
        }

        var speechChunks = new List<byte[]>(capacity: 10);
        for (var i = 0; i < 10; i++)
        {
            var chunk = CreateSpeechChunkMs(20);
            speechChunks.Add(chunk);
            segmenter.ProcessChunk(chunk);
        }

        PcmSpeechCaptureSegmenterUpdate update = default;
        for (var i = 0; i < 150; i++)
        {
            update = segmenter.ProcessChunk(CreateSilenceChunkMs(20));
            if (update.CompletedUtterance is not null)
            {
                break;
            }
        }

        Assert.NotNull(update.CompletedUtterance);
        var chunks = update.CompletedUtterance!.Chunks;

        // Der Vorlauf muss vor den Sprachblöcken stehen, ohne den ersten Sprachblock zu verdoppeln.
        for (var i = 0; i < silenceChunks.Count; i++)
        {
            Assert.Same(silenceChunks[i], chunks[i]);
        }

        Assert.Same(speechChunks[0], chunks[silenceChunks.Count]);
        Assert.Same(speechChunks[1], chunks[silenceChunks.Count + 1]);
    }

    [Fact]
    public void ProcessChunk_WhenSpeechStarts_RetainsAtLeastQuarterSecondOfPreOnsetAudio()
    {
        // Vorlauf erhält auch Audio vor dem Überschreiten der Sprachschwelle, etwa leise Anlaute.
        const int chunkMs = 20;
        const int minLeadInMs = 250;
        const int debounceMs = 3000;
        var segmenter = new PcmSpeechCaptureSegmenter(SpeechAudioMimeType.Pcm, silenceDebounceMs: debounceMs, SegmenterOptions);

        // Mehr als die geforderte Mindestdauer an Vorlauf bereitstellen.
        const int preOnsetChunks = 20;
        var preOnsetSilence = new List<byte[]>(capacity: preOnsetChunks);
        for (var i = 0; i < preOnsetChunks; i++)
        {
            var chunk = CreateSilenceChunkMs(chunkMs);
            preOnsetSilence.Add(chunk);
            segmenter.ProcessChunk(chunk);
        }

        var firstSpeechChunk = CreateSpeechChunkMs(chunkMs);
        segmenter.ProcessChunk(firstSpeechChunk);
        for (var i = 0; i < 9; i++)
        {
            segmenter.ProcessChunk(CreateSpeechChunkMs(chunkMs));
        }

        PcmSpeechCaptureSegmenterUpdate update = default;
        for (var i = 0; i < 150; i++)
        {
            update = segmenter.ProcessChunk(CreateSilenceChunkMs(chunkMs));
            if (update.CompletedUtterance is not null)
            {
                break;
            }
        }

        Assert.NotNull(update.CompletedUtterance);
        var chunks = update.CompletedUtterance!.Chunks;

        var firstSpeechIndex = chunks.ToList().FindIndex(chunk => ReferenceEquals(chunk, firstSpeechChunk));
        Assert.True(firstSpeechIndex >= 0, "The first speech chunk must appear in the completed segment.");

        var leadInMs = firstSpeechIndex * chunkMs;
        Assert.True(
            leadInMs >= minLeadInMs,
            $"Expected at least {minLeadInMs}ms of pre-onset lead-in, but only {leadInMs}ms preceded the first speech chunk.");
    }

    [Fact]
    public void ProcessChunk_WhenWakePhaseDebounceBridgesInterWordPause_KeepsWakeWordAndCommandInOneSegment()
    {
        // Zwei Signalabschnitte modellieren Aktivierungswort und Befehl mit einer Pause dazwischen.
        const int wakeDebounceMs = 900;
        var segmenter = new PcmSpeechCaptureSegmenter(SpeechAudioMimeType.Pcm, silenceDebounceMs: wakeDebounceMs, SegmenterOptions);

        for (var i = 0; i < 10; i++)
        {
            segmenter.ProcessChunk(CreateSpeechChunkMs(20));
        }

        // Die 600-ms-Pause liegt unter der hier gesetzten Stillegrenze von 900 ms.
        for (var i = 0; i < 30; i++)
        {
            var pauseUpdate = segmenter.ProcessChunk(CreateSilenceChunkMs(20));
            Assert.Null(pauseUpdate.CompletedUtterance);
            Assert.Null(pauseUpdate.CompletionException);
        }

        for (var i = 0; i < 10; i++)
        {
            var commandUpdate = segmenter.ProcessChunk(CreateSpeechChunkMs(20));
            Assert.Null(commandUpdate.CompletedUtterance);
        }

        PcmSpeechCaptureSegmenterUpdate update = default;
        for (var i = 0; i < 150; i++)
        {
            update = segmenter.ProcessChunk(CreateSilenceChunkMs(20));
            if (update.CompletedUtterance is not null)
            {
                break;
            }
        }

        Assert.NotNull(update.CompletedUtterance);
        Assert.Null(update.CompletionException);

        // Der Umfang muss über einen einzelnen Sprachabschnitt samt Nachlauf hinausgehen.
        Assert.True(update.CompletedUtterance!.Chunks.Count > 40);
    }

    [Fact]
    public void ProcessChunk_WhenPauseExceedsWakePhaseDebounce_CompletesBeforeCommand()
    {
        // Eine Pause über der Stillegrenze muss das Segment abschließen.
        const int wakeDebounceMs = 900;
        var segmenter = new PcmSpeechCaptureSegmenter(SpeechAudioMimeType.Pcm, silenceDebounceMs: wakeDebounceMs, SegmenterOptions);

        for (var i = 0; i < 10; i++)
        {
            segmenter.ProcessChunk(CreateSpeechChunkMs(20));
        }

        PcmSpeechCaptureSegmenterUpdate update = default;
        var completedDuringPause = false;
        for (var i = 0; i < 60; i++)
        {
            update = segmenter.ProcessChunk(CreateSilenceChunkMs(20));
            if (update.CompletedUtterance is not null)
            {
                completedDuringPause = true;
                break;
            }
        }

        Assert.True(completedDuringPause);
        Assert.NotNull(update.CompletedUtterance);
    }

    [Fact]
    public void ProcessChunk_WhenSilenceExceedsMaxInitialSilence_CompletesWithNoSpeechDetected()
    {
        var segmenter = new PcmSpeechCaptureSegmenter(SpeechAudioMimeType.Pcm, silenceDebounceMs: 3000, SegmenterOptions);

        PcmSpeechCaptureSegmenterUpdate update = new(false, null, null);

        for (var i = 0; i < 15; i++)
        {
            update = segmenter.ProcessChunk(CreateSilenceChunkMs(1000));
            if (update.CompletionException is not null)
            {
                break;
            }
        }

        Assert.IsType<NoSpeechDetectedException>(update.CompletionException);
        Assert.Null(update.CompletedUtterance);
    }

    [Fact]
    public void ProcessChunk_WhenOnsetBeginsNearInitialSilenceDeadlineThenDips_DoesNotOrphanUtteranceAsNoSpeech()
    {
        // Ein Kandidat kurz vor Ablauf der Anfangsstille darf durch eine kurze Unterbrechung nicht sofort als NoSpeech enden.
        var options = new PcmSpeechCaptureSegmenterOptions
        {
            MaxInitialSilence = TimeSpan.FromMilliseconds(100),
            SpeechStartConfirmation = TimeSpan.FromMilliseconds(120),
        };
        var segmenter = new PcmSpeechCaptureSegmenter(SpeechAudioMimeType.Pcm, silenceDebounceMs: 200, options);

        for (var i = 0; i < 4; i++)
        {
            var silence = segmenter.ProcessChunk(CreateSilenceChunkMs(20));
            Assert.Null(silence.CompletionException);
        }

        // Der Kandidat hat die Schwelle überschritten, ist aber noch nicht als Sprachbeginn bestätigt.
        var onset = segmenter.ProcessChunk(CreateSpeechChunkMs(20));
        Assert.Null(onset.CompletionException);

        // Die kurze Stille unterbricht den Kandidaten vor seiner Bestätigung.
        var dip = segmenter.ProcessChunk(CreateSilenceChunkMs(20));

        Assert.Null(dip.CompletionException);

        for (var i = 0; i < 6; i++)
        {
            segmenter.ProcessChunk(CreateSpeechChunkMs(20));
        }

        PcmSpeechCaptureSegmenterUpdate completed = default;
        for (var i = 0; i < 200; i++)
        {
            completed = segmenter.ProcessChunk(CreateSilenceChunkMs(20));
            if (completed.CompletedUtterance is not null)
            {
                break;
            }

            Assert.Null(completed.CompletionException);
        }

        Assert.NotNull(completed.CompletedUtterance);
    }

    [Fact]
    public void ProcessChunk_WhenCaptureStartsDuringPlayback_DoesNotTreatPromptLeakageAsSpeech()
    {
        var segmenter = new PcmSpeechCaptureSegmenter(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 3000,
            SpeechCaptureStartContext.AssistantPlaybackActive,
            SegmenterOptions);

        var first = segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 600));
        var second = segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 600));
        var third = segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 600));

        Assert.False(first.SpeechStarted);
        Assert.False(second.SpeechStarted);
        Assert.False(third.SpeechStarted);
        Assert.Null(first.CompletedUtterance);
        Assert.Null(second.CompletedUtterance);
        Assert.Null(third.CompletedUtterance);
        Assert.Null(first.CompletionException);
        Assert.Null(second.CompletionException);
        Assert.Null(third.CompletionException);
    }

    [Fact]
    public void ProcessChunk_WhenCaptureStartsDuringPlayback_TriggersOnSpeechAboveLeakageFloor()
    {
        var segmenter = new PcmSpeechCaptureSegmenter(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 3000,
            SpeechCaptureStartContext.AssistantPlaybackActive,
            SegmenterOptions);

        for (var i = 0; i < SegmenterOptions.ActivePlaybackCalibrationChunkCount; i++)
        {
            segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 600));
        }

        PcmSpeechCaptureSegmenterUpdate update = default;
        for (var i = 0; i < 6; i++)
        {
            update = segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 1400));
        }

        Assert.True(update.SpeechStarted);
        Assert.Null(update.CompletedUtterance);
        Assert.Null(update.CompletionException);
    }

    [Fact]
    public void ProcessChunk_WhenCaptureStartsDuringPlayback_DoesNotConfirmSpeechFromSingleLeakageSpike()
    {
        var segmenter = new PcmSpeechCaptureSegmenter(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 3000,
            SpeechCaptureStartContext.AssistantPlaybackActive,
            SegmenterOptions);

        PcmSpeechCaptureSegmenterUpdate first = default;
        PcmSpeechCaptureSegmenterUpdate second = default;
        for (var i = 0; i < SegmenterOptions.ActivePlaybackCalibrationChunkCount; i++)
        {
            var update = segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 600));
            if (i == 0)
            {
                first = update;
            }

            if (i == 1)
            {
                second = update;
            }
        }

        var spike = segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 1100));
        var fourth = segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 600));
        var fifth = segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 600));

        Assert.False(first.SpeechStarted);
        Assert.False(second.SpeechStarted);
        Assert.False(spike.SpeechStarted);
        Assert.False(fourth.SpeechStarted);
        Assert.False(fifth.SpeechStarted);
        Assert.Null(spike.CompletedUtterance);
        Assert.Null(fourth.CompletedUtterance);
        Assert.Null(fifth.CompletedUtterance);
        Assert.Null(spike.CompletionException);
        Assert.Null(fourth.CompletionException);
        Assert.Null(fifth.CompletionException);
    }

    [Fact]
    public void ProcessChunk_WhenCaptureStartsAfterRecentPlaybackWithContinuation_DelaysSpeechStartUntilRecentPlaybackGuardExpires()
    {
        var options = new PcmSpeechCaptureSegmenterOptions
        {
            RecentPlaybackSpeechStartGuard = TimeSpan.FromMilliseconds(60)
        };
        var segmenter = new PcmSpeechCaptureSegmenter(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 3000,
            SpeechCaptureStartContext.AssistantPlaybackRecentlySettledContinuation,
            options);

        for (var i = 0; i < 3; i++)
        {
            var guarded = segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 1000));
            Assert.False(guarded.SpeechStarted);
            Assert.Null(guarded.CompletedUtterance);
            Assert.Null(guarded.CompletionException);
        }

        PcmSpeechCaptureSegmenterUpdate update = default;
        for (var i = 0; i < 6; i++)
        {
            update = segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 1000));
        }

        Assert.True(update.SpeechStarted);
        Assert.Null(update.CompletedUtterance);
        Assert.Null(update.CompletionException);
    }

    [Fact]
    public void ProcessChunk_WhenCaptureStartsAfterRecentPlaybackWithoutContinuation_CalibratesAwayResidualLeakage()
    {
        var options = new PcmSpeechCaptureSegmenterOptions
        {
            RecentPlaybackSpeechStartGuard = TimeSpan.FromMilliseconds(40)
        };
        var segmenter = new PcmSpeechCaptureSegmenter(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 3000,
            SpeechCaptureStartContext.AssistantPlaybackRecentlySettledIdle,
            options);

        var first = segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 600));
        var second = segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 600));
        var third = segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 600));

        Assert.False(first.SpeechStarted);
        Assert.False(second.SpeechStarted);
        Assert.False(third.SpeechStarted);

        PcmSpeechCaptureSegmenterUpdate update = default;
        for (var i = 0; i < 6; i++)
        {
            update = segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 1000));
        }

        Assert.True(update.SpeechStarted);
        Assert.Null(update.CompletedUtterance);
        Assert.Null(update.CompletionException);
    }

    [Fact]
    public void ProcessChunk_WhenIdlePostPlaybackFloorInflatesFromBleed_QuietShortWordStillConfirmsOnset()
    {
        // Nach hoher Kalibrierungsamplitude muss im Idle-Kontext auch die niedrigere Testamplitude einen Sprachbeginn auslösen können.
        var options = new PcmSpeechCaptureSegmenterOptions
        {
            RecentPlaybackSpeechStartGuard = TimeSpan.FromMilliseconds(40)
        };
        var segmenter = new PcmSpeechCaptureSegmenter(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 3000,
            SpeechCaptureStartContext.AssistantPlaybackRecentlySettledIdle,
            options);

        // Hohe Amplitude während der Schutzzeit und der anschließenden Kalibrierung einspeisen.
        for (var i = 0; i < 4; i++)
        {
            segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 950));
        }

        // Das nur einmal gesetzte SpeechStarted-Signal über alle Prüfblöcke sammeln.
        var speechStarted = false;
        for (var i = 0; i < 12; i++)
        {
            var update = segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 800));
            speechStarted |= update.SpeechStarted;
            Assert.Null(update.CompletionException);
        }

        Assert.True(speechStarted);
    }

    [Fact]
    public void ProcessChunk_WhenCaptureStartsDuringPlayback_IdleFloorCapDoesNotLowerBargeInThreshold()
    {
        // Bei aktiver Wiedergabe darf dieselbe Begrenzung wie im Idle-Kontext keinen zu frühen Sprachbeginn auslösen.
        var segmenter = new PcmSpeechCaptureSegmenter(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 3000,
            SpeechCaptureStartContext.AssistantPlaybackActive,
            SegmenterOptions);

        for (var i = 0; i < SegmenterOptions.ActivePlaybackCalibrationChunkCount; i++)
        {
            segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 600));
        }

        var speechStarted = false;
        for (var i = 0; i < 12; i++)
        {
            var update = segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 700));
            speechStarted |= update.SpeechStarted;
            Assert.Null(update.CompletionException);
        }

        Assert.False(speechStarted);
    }

    [Fact]
    public void ProcessChunk_WhenCaptureStartsDuringPlayback_UsesExtendedCalibrationWindowForRisingPromptLeakage()
    {
        var segmenter = new PcmSpeechCaptureSegmenter(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 3000,
            SpeechCaptureStartContext.AssistantPlaybackActive,
            SegmenterOptions);

        for (var i = 0; i < SegmenterOptions.ActivePlaybackCalibrationChunkCount; i++)
        {
            var sampleValue = (short)(500 + (i * 100));
            var update = segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: sampleValue));
            Assert.False(update.SpeechStarted);
            Assert.Null(update.CompletedUtterance);
            Assert.Null(update.CompletionException);
        }

        var followUp = segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 1200));

        Assert.False(followUp.SpeechStarted);
        Assert.Null(followUp.CompletedUtterance);
        Assert.Null(followUp.CompletionException);
    }

    [Fact]
    public void ProcessChunk_WhenCaptureStartsDuringPlayback_CalibratesAgainstLoudPromptLeakage()
    {
        var segmenter = new PcmSpeechCaptureSegmenter(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 3000,
            SpeechCaptureStartContext.AssistantPlaybackActive,
            SegmenterOptions);

        var first = segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 1200));
        var second = segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 1200));
        var third = segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 1200));

        Assert.False(first.SpeechStarted);
        Assert.False(second.SpeechStarted);
        Assert.False(third.SpeechStarted);
        Assert.Null(first.CompletedUtterance);
        Assert.Null(second.CompletedUtterance);
        Assert.Null(third.CompletedUtterance);
        Assert.Null(first.CompletionException);
        Assert.Null(second.CompletionException);
        Assert.Null(third.CompletionException);
    }

    [Fact]
    public void ProcessChunk_WhenCaptureStartsDuringPlayback_DoesNotIncludePromptPreRollInCompletedUtterance()
    {
        const int debounceMs = 3000;
        var segmenter = new PcmSpeechCaptureSegmenter(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: debounceMs,
            SpeechCaptureStartContext.AssistantPlaybackActive,
            SegmenterOptions);

        var promptChunks = Enumerable.Range(0, SegmenterOptions.ActivePlaybackCalibrationChunkCount)
            .Select(_ => CreatePcmChunk(durationMs: 20, sampleValue: 600))
            .ToArray();
        var speechChunkA = CreatePcmChunk(durationMs: 20, sampleValue: 1400);
        var speechChunkB = CreatePcmChunk(durationMs: 20, sampleValue: 1400);

        foreach (var promptChunk in promptChunks)
        {
            segmenter.ProcessChunk(promptChunk);
        }

        segmenter.ProcessChunk(speechChunkA);
        segmenter.ProcessChunk(speechChunkB);
        for (var i = 0; i < 4; i++)
        {
            segmenter.ProcessChunk(CreatePcmChunk(durationMs: 20, sampleValue: 1400));
        }

        PcmSpeechCaptureSegmenterUpdate update = default;
        for (var i = 0; i < 150; i++)
        {
            update = segmenter.ProcessChunk(CreateSilenceChunkMs(20));
            if (update.CompletedUtterance is not null)
            {
                break;
            }
        }

        Assert.NotNull(update.CompletedUtterance);

        var chunks = update.CompletedUtterance!.Chunks;
        Assert.Same(speechChunkA, chunks[0]);
        foreach (var promptChunk in promptChunks)
        {
            Assert.DoesNotContain(promptChunk, chunks);
        }
    }

    [Fact]
    public void ReadFrom_WhenShortSpeechIsBufferedInsideSilence_DetectsSpeechAtLiveFrameGranularity()
    {
        var options = new PcmSpeechCaptureSegmenterOptions
        {
            SpeechStartConfirmation = TimeSpan.FromMilliseconds(40),
            TailSilence = TimeSpan.FromMilliseconds(40)
        };
        var ring = new InMemorySpeechSampleRing();
        var cursor = ring.OpenCursor(SampleCursorOrigin.Latest);
        var segmenter = new PcmSpeechCaptureSegmenter(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 40,
            options);

        ring.WriteSamples(CreateSilenceChunkMs(360));
        ring.WriteSamples(CreatePcmChunk(durationMs: 150, sampleValue: SpeechSampleValue));
        ring.WriteSamples(CreateSilenceChunkMs(200));

        CapturedSpeechAudio? completed = null;
        for (var i = 0; i < 64; i++)
        {
            var before = cursor;
            var update = segmenter.ReadFrom(ring, ref cursor);

            Assert.Null(update.CompletionException);
            completed ??= update.CompletedUtterance;

            if (cursor.SampleIndex == before.SampleIndex || completed is not null)
            {
                break;
            }
        }

        Assert.NotNull(completed);
    }

    private static byte[] CreateSilenceChunkMs(int durationMs)
    {
        var samples = durationMs * SampleRateHz / 1000;
        return new byte[samples * BytesPerSample];
    }

    private static byte[] CreateSpeechChunkMs(int durationMs)
        => CreatePcmChunk(durationMs, SpeechSampleValue);

    private static byte[] CreatePcmChunk(int durationMs, short sampleValue)
    {
        var samples = durationMs * SampleRateHz / 1000;
        var buffer = new byte[samples * BytesPerSample];

        for (var i = 0; i < samples; i++)
        {
            var offset = i * BytesPerSample;
            BinaryPrimitives.WriteInt16LittleEndian(buffer.AsSpan(offset, BytesPerSample), sampleValue);
        }

        return buffer;
    }
}
