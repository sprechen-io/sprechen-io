using System.Buffers.Binary;
using Sprechen.Proto;

namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Vergleicht die Erkennung kurzer synthetischer Signale nach Wiedergabeende mit verschiedenen
/// Startschutz- und Bestätigungszeiten. Die Tests verwenden konstante PCM-Amplituden; sie prüfen
/// weder gesprochene Bestätigungswörter noch die akustische Wirkung eines Earcons.
/// </summary>
public sealed class ShortConfirmationWordOnsetClipTests
{
    private const int SampleRateHz = 16000;
    private const int BytesPerSample = 2;
    private const int ChunkMs = 20;

    // Die hohe Testamplitude soll den Einfluss der Zeitgrenzen von der Kalibrierung trennen.
    private const short ShortWordAmplitude = 1400;

    // Zehn Blöcke ergeben ein 200-ms-Signal.
    private const int ShortWordChunks = 10;

    // Das 360-ms-Vergleichssignal ist länger als Startschutz und Bestätigungszeit zusammen.
    private const int NormalWordChunks = 18;

    [Fact]
    public void ProcessChunk_WhenShortConfirmationWordFollowsEarcon_UnderDefaultOnset_NeverConfirmsAndYieldsNoSpeechDetected()
    {
        var segmenter = CreateConfirmTurnSegmenter(new PcmSpeechCaptureSegmenterOptions());

        for (var i = 0; i < ShortWordChunks; i++)
        {
            var update = segmenter.ProcessChunk(CreatePcmChunk(ChunkMs, ShortWordAmplitude));

            // Nach 160 ms Startschutz reicht der Rest des 200-ms-Signals nicht für 120 ms Bestätigung.
            Assert.False(update.SpeechStarted);
            Assert.Null(update.CompletedUtterance);
            Assert.Null(update.CompletionException);
        }

        // Ohne bestätigten Sprachbeginn endet die anschließende Stille mit NoSpeech.
        PcmSpeechCaptureSegmenterUpdate timeout = default;
        for (var i = 0; i < 20; i++)
        {
            timeout = segmenter.ProcessChunk(CreateSilenceChunkMs(1000));
            if (timeout.CompletionException is not null)
            {
                break;
            }
        }

        Assert.IsType<NoSpeechDetectedException>(timeout.CompletionException);
        Assert.Null(timeout.CompletedUtterance);
    }

    [Fact]
    public void ProcessChunk_WhenShortConfirmationWordFollowsEarcon_WithRelaxedRecentPlaybackGuard_CapturesTheWord()
    {
        // Nur den Startschutz verkürzen; die Bestätigungszeit bleibt auf dem Standardwert.
        var confirmTurnOptions = new PcmSpeechCaptureSegmenterOptions
        {
            RecentPlaybackSpeechStartGuard = TimeSpan.FromMilliseconds(40)
        };
        var segmenter = CreateConfirmTurnSegmenter(confirmTurnOptions);

        PcmSpeechCaptureSegmenterUpdate confirmed = default;
        for (var i = 0; i < ShortWordChunks; i++)
        {
            var update = segmenter.ProcessChunk(CreatePcmChunk(ChunkMs, ShortWordAmplitude));
            if (update.SpeechStarted)
            {
                confirmed = update;
                break;
            }
        }

        Assert.True(confirmed.SpeechStarted);
        Assert.Null(confirmed.CompletionException);
    }

    [Fact]
    public void ProcessChunk_WhenShortConfirmationWordFollowsEarcon_WithRelaxedSpeechStartConfirmationOnly_CapturesTheWordButOnlyMarginally()
    {
        // Hier nur die Bestätigungszeit verkürzen und den längeren Startschutz beibehalten.
        var confirmTurnOptions = new PcmSpeechCaptureSegmenterOptions
        {
            SpeechStartConfirmation = TimeSpan.FromMilliseconds(20)
        };
        var segmenter = CreateConfirmTurnSegmenter(confirmTurnOptions);

        PcmSpeechCaptureSegmenterUpdate confirmed = default;
        for (var i = 0; i < ShortWordChunks; i++)
        {
            var update = segmenter.ProcessChunk(CreatePcmChunk(ChunkMs, ShortWordAmplitude));
            if (update.SpeechStarted)
            {
                confirmed = update;
                break;
            }
        }

        Assert.True(confirmed.SpeechStarted);
        Assert.Null(confirmed.CompletionException);
    }

    [Fact]
    public void ProcessChunk_WhenShortConfirmationWordFollowsEarcon_UnderShortConfirmationModeOptions_ConfirmsTheWord()
    {
        // Die Optionen des Modus ShortConfirmation mit der produktiven Factory erzeugen.
        var options = CreateModeOptions(SpeechCaptureMode.ShortConfirmation);
        Assert.Equal(TimeSpan.FromMilliseconds(60), options.RecentPlaybackSpeechStartGuard);
        Assert.Equal(TimeSpan.FromMilliseconds(120), options.SpeechStartConfirmation);
        var segmenter = CreateConfirmTurnSegmenter(options);

        PcmSpeechCaptureSegmenterUpdate confirmed = default;
        for (var i = 0; i < ShortWordChunks; i++)
        {
            var update = segmenter.ProcessChunk(CreatePcmChunk(ChunkMs, ShortWordAmplitude));
            if (update.SpeechStarted)
            {
                confirmed = update;
                break;
            }
        }

        Assert.True(confirmed.SpeechStarted);
        Assert.Null(confirmed.CompletionException);
    }

    [Fact]
    public void ProcessChunk_WhenPureLeakageBleedFollowsEarcon_UnderShortConfirmationModeOptions_StillRejected()
    {
        // Auch mit ShortConfirmation darf die niedrigere Testamplitude keinen Sprachbeginn auslösen.
        const short leakageAmplitude = 600;
        var segmenter = CreateConfirmTurnSegmenter(CreateModeOptions(SpeechCaptureMode.ShortConfirmation));

        for (var i = 0; i < 16; i++)
        {
            var update = segmenter.ProcessChunk(CreatePcmChunk(ChunkMs, leakageAmplitude));
            Assert.False(update.SpeechStarted);
            Assert.Null(update.CompletedUtterance);
            Assert.Null(update.CompletionException);
        }
    }

    [Fact]
    public void ProcessChunk_WhenShortConfirmationWordFollowsEarcon_UnderListeningModeOptions_IsStillClipped()
    {
        // Listening behält in diesem Vergleich den längeren Startschutz.
        var options = CreateModeOptions(SpeechCaptureMode.Listening);
        Assert.Equal(TimeSpan.FromMilliseconds(160), options.RecentPlaybackSpeechStartGuard);
        var segmenter = CreateConfirmTurnSegmenter(options);

        var confirmedAny = false;
        for (var i = 0; i < ShortWordChunks; i++)
        {
            if (segmenter.ProcessChunk(CreatePcmChunk(ChunkMs, ShortWordAmplitude)).SpeechStarted)
            {
                confirmedAny = true;
                break;
            }
        }

        Assert.False(confirmedAny);
    }

    [Fact]
    public void ProcessChunk_WhenNormalLengthWordFollowsEarcon_UnderDefaultOnset_StillCaptures()
    {
        // Das längere Vergleichssignal soll auch mit den Standardzeiten bestätigt werden.
        var segmenter = CreateConfirmTurnSegmenter(new PcmSpeechCaptureSegmenterOptions());

        PcmSpeechCaptureSegmenterUpdate confirmed = default;
        for (var i = 0; i < NormalWordChunks; i++)
        {
            var update = segmenter.ProcessChunk(CreatePcmChunk(ChunkMs, ShortWordAmplitude));
            if (update.SpeechStarted)
            {
                confirmed = update;
                break;
            }
        }

        Assert.True(confirmed.SpeechStarted);
        Assert.Null(confirmed.CompletionException);
    }

    [Fact]
    public void ProcessChunk_WhenPureLeakageBleedFollowsEarcon_UnderDefaultFieldCaptureOptions_StillRejected()
    {
        // Die niedrigere Testamplitude soll auch mit Standardoptionen ohne bestätigten Sprachbeginn bleiben.
        const short leakageAmplitude = 600;
        var segmenter = CreateConfirmTurnSegmenter(new PcmSpeechCaptureSegmenterOptions());

        for (var i = 0; i < 16; i++)
        {
            var update = segmenter.ProcessChunk(CreatePcmChunk(ChunkMs, leakageAmplitude));
            Assert.False(update.SpeechStarted);
            Assert.Null(update.CompletedUtterance);
            Assert.Null(update.CompletionException);
        }
    }

    // Die produktive Factory mit den Defaultwerten beider Optionsklassen aufrufen.
    private static PcmSpeechCaptureSegmenterOptions CreateModeOptions(SpeechCaptureMode mode)
    {
        var factory = new SpeechCaptureSegmenterOptionsFactory(
            Options.Create(new SpeechCaptureClientOptions()),
            Options.Create(new PcmSpeechCaptureSegmenterOptions()));
        return factory.CreateOptions(SpeechCaptureModeBehavior.For(mode), initialSilenceTimeout: null);
    }

    private static PcmSpeechCaptureSegmenter CreateConfirmTurnSegmenter(PcmSpeechCaptureSegmenterOptions options)
        => new(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 3000,
            SpeechCaptureStartContext.AssistantPlaybackRecentlySettledIdle,
            options);

    private static byte[] CreateSilenceChunkMs(int durationMs)
    {
        var samples = durationMs * SampleRateHz / 1000;
        return new byte[samples * BytesPerSample];
    }

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
