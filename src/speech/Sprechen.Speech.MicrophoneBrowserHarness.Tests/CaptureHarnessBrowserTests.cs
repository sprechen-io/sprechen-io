using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Microsoft.Playwright;
using Xunit;

namespace Sprechen.Speech.MicrophoneBrowserHarness.Tests;

public sealed class CaptureHarnessBrowserTests
{
    [Fact(Timeout = 180_000)]
    public async Task SyntheticBrowserAudioReachesTheAppConsumer()
    {
        var publishDir = Environment.GetEnvironmentVariable("HARNESS_PUBLISH_DIR");
        var expectedHash = Environment.GetEnvironmentVariable("MIC_CAPTURE_JS_SHA256");
        var wavDir = Environment.GetEnvironmentVariable("HARNESS_WAV_DIR");
        Assert.False(string.IsNullOrWhiteSpace(publishDir), "HARNESS_PUBLISH_DIR is required.");
        Assert.False(string.IsNullOrWhiteSpace(expectedHash), "MIC_CAPTURE_JS_SHA256 is required.");
        Assert.False(string.IsNullOrWhiteSpace(wavDir), "HARNESS_WAV_DIR is required.");
        Assert.False(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH")), "PLAYWRIGHT_BROWSERS_PATH is required.");

        var capturePath = Path.Combine(publishDir, "_content", "MicroSharp.Browser", "microsharp-capture.js");
        var publishedBytes = await File.ReadAllBytesAsync(capturePath);
        Assert.Equal(expectedHash, Sha256(publishedBytes));
        var publishedText = System.Text.Encoding.UTF8.GetString(publishedBytes);
        Assert.DoesNotContain("micRecorder", publishedText, StringComparison.Ordinal);
        var index = await File.ReadAllTextAsync(Path.Combine(publishDir, "index.html"));
        Assert.DoesNotContain("microphoneRecorder.js", index, StringComparison.Ordinal);
        Assert.DoesNotContain("pcmRecorderProcessor.js", index, StringComparison.Ordinal);

        Directory.CreateDirectory(wavDir);
        var wavPath = Path.Combine(wavDir, "harness-tone.wav");
        WriteSine(wavPath, 48_000, 440, seconds: 3, amplitude: 0.8);

        await using var server = new PublishedHarnessServer(publishDir);
        using var http = new HttpClient();
        var served = await http.GetByteArrayAsync(new Uri(server.Root, "_content/MicroSharp.Browser/microsharp-capture.js"));
        Assert.Equal(expectedHash, Sha256(served));

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args =
            [
                "--use-fake-device-for-media-stream",
                "--use-fake-ui-for-media-stream",
                $"--use-file-for-fake-audio-capture={wavPath}",
                "--autoplay-policy=no-user-gesture-required",
            ],
        });
        var page = await browser.NewPageAsync();
        var requests = new List<string>();
        var console = new List<string>();
        page.Request += (_, request) => requests.Add(request.Url);
        page.Console += (_, message) => console.Add(message.Type + ": " + message.Text);
        page.PageError += (_, message) => console.Add("pageerror: " + message);
        await page.GotoAsync(server.Root.AbsoluteUri);
        await WaitForPhase(page, "ready", console);
        var ready = await Status(page);
        Assert.Contains("sameInstance=true", ready, StringComparison.Ordinal);

        await page.ClickAsync("#request-access");
        await WaitForPhase(page, "access-held", console);
        var held = await WaitForProbe(page, probe => probe.GumCount == 1 && probe.Tracks.Count > 0 && probe.Tracks[0].ReadyState == "live");
        Assert.Equal(1, held.ContextCount);
        Assert.Contains("running", held.Contexts);
        Assert.True(held.Tracks[0].Enabled);

        var changes = held.EnabledChanges.Count;
        await page.ClickAsync("#close-gate");
        await WaitForPhase(page, "gate-closed", console);
        var closed = await WaitForProbe(page, probe => probe.EnabledChanges.Skip(changes).Contains(false) && probe.Tracks[0].Enabled == false);
        Assert.False(closed.Tracks[0].Enabled);

        await page.ClickAsync("#start");
        await WaitForPhase(page, "recording", console);
        var startedMuted = await ReadProbe(page);
        Assert.False(startedMuted.Tracks[0].Enabled);
        Assert.Equal(1, startedMuted.GumCount);

        changes = startedMuted.EnabledChanges.Count;
        await page.ClickAsync("#open-gate");
        await WaitForPhase(page, "gate-open", console);
        await page.WaitForFunctionAsync(
            "() => { const text = document.querySelector('#harness-status')?.textContent ?? ''; return text.includes('nonzeroPcm=true') && text.includes('speechStarted=true') && !text.includes('ringBytes=0'); }",
            null,
            new PageWaitForFunctionOptions { Timeout = 20_000 });
        var open = await WaitForProbe(page, probe => probe.EnabledChanges.Skip(changes).Contains(true) && probe.Tracks[0].Enabled);
        Assert.True(open.Tracks[0].Enabled);
        var withAudio = await Status(page);
        Assert.Contains("nonzeroPcm=true", withAudio, StringComparison.Ordinal);
        var chunksBeforeClose = FieldInt(withAudio, "chunkCount");
        Assert.True(chunksBeforeClose > 0);

        changes = open.EnabledChanges.Count;
        await page.ClickAsync("#close-gate");
        await WaitForPhase(page, "gate-closed", console);
        var closedWhileRecording = await WaitForProbe(page, probe => probe.EnabledChanges.Skip(changes).Contains(false) && !probe.Tracks[0].Enabled);
        Assert.False(closedWhileRecording.Tracks[0].Enabled);

        changes = closedWhileRecording.EnabledChanges.Count;
        await page.ClickAsync("#open-gate");
        await WaitForPhase(page, "gate-open", console);
        var reopened = await WaitForProbe(page, probe => probe.EnabledChanges.Skip(changes).Contains(true) && probe.Tracks[0].Enabled);
        Assert.True(reopened.Tracks[0].Enabled);
        Assert.Equal(1, reopened.GumCount);
        Assert.Equal(1, reopened.ContextCount);

        await page.ClickAsync("#stop");
        await WaitForPhase(page, "stopped", console);
        var stopped = await ReadProbe(page);
        Assert.Equal(1, stopped.GumCount);
        Assert.Equal("live", stopped.Tracks[0].ReadyState);
        Assert.DoesNotContain("closed", stopped.Contexts);
        var stoppedStatus = await Status(page);
        var chunksAtStop = FieldInt(stoppedStatus, "chunkCount");

        await page.ClickAsync("#start");
        await WaitForPhase(page, "recording", console);
        await page.WaitForFunctionAsync(
            $"() => {{ const text = document.querySelector('#harness-status')?.textContent ?? ''; const match = text.match(/chunkCount=(\\d+)/); return match && Number(match[1]) > {chunksAtStop}; }}",
            null,
            new PageWaitForFunctionOptions { Timeout = 20_000 });
        var restarted = await ReadProbe(page);
        Assert.Equal(1, restarted.GumCount);
        Assert.Equal(1, restarted.ContextCount);
        Assert.Equal("live", restarted.Tracks[0].ReadyState);

        await page.ClickAsync("#release");
        await WaitForPhase(page, "released", console);
        var released = await WaitForProbe(page, probe =>
            probe.Tracks.Count > 0
            && probe.Tracks.All(track => track.ReadyState == "ended")
            && probe.Contexts.Count > 0
            && probe.Contexts.All(state => state == "closed"));
        Assert.Equal(1, released.GumCount);
        var releasedStatus = await Status(page);
        Assert.Contains("error=", releasedStatus, StringComparison.Ordinal);
        Assert.DoesNotContain("error=Microphone", releasedStatus, StringComparison.Ordinal);

        Assert.DoesNotContain(requests, url => url.Contains("microphoneRecorder.js", StringComparison.Ordinal));
        Assert.DoesNotContain(requests, url => url.Contains("pcmRecorderProcessor.js", StringComparison.Ordinal));
        Assert.Contains(requests, url => url.Contains("/_content/MicroSharp.Browser/microsharp-capture.js", StringComparison.Ordinal));
        Assert.Contains(server.Paths, path => path.Contains("pcm-recorder-processor.js", StringComparison.Ordinal));
        Assert.DoesNotContain(server.Paths, path => path.Contains("microphoneRecorder.js", StringComparison.Ordinal));
    }

    private static async Task WaitForPhase(IPage page, string phase, IReadOnlyList<string> console)
    {
        try
        {
            await page.WaitForFunctionAsync(
                $"() => (document.querySelector('#harness-status')?.textContent ?? '').includes('phase={phase}')",
                null,
                new PageWaitForFunctionOptions { Timeout = 30_000 });
        }
        catch (TimeoutException exception)
        {
            var body = await page.Locator("body").InnerTextAsync();
            throw new TimeoutException(
                $"phase={phase} was not reached. Body={body} Console={string.Join(" | ", console)}",
                exception);
        }

        var status = await Status(page);
        Assert.DoesNotContain("error=Microphone", status, StringComparison.Ordinal);
        Assert.DoesNotContain("error=Invalid", status, StringComparison.Ordinal);
        Assert.DoesNotContain("error=JS", status, StringComparison.Ordinal);
    }

    private static async Task<string> Status(IPage page) => await page.Locator("#harness-status").InnerTextAsync();

    private static async Task<ProbeSnapshot> ReadProbe(IPage page)
    {
        var json = await page.Locator("#mic-probe").InnerTextAsync();
        var probe = System.Text.Json.JsonSerializer.Deserialize<ProbeSnapshot>(json);
        return probe ?? throw new InvalidOperationException("The microphone probe was empty.");
    }

    private static async Task<ProbeSnapshot> WaitForProbe(IPage page, Func<ProbeSnapshot, bool> predicate)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        ProbeSnapshot? last = null;
        while (DateTime.UtcNow < deadline)
        {
            last = await ReadProbe(page);
            if (predicate(last))
            {
                return last;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException($"Probe did not match. Last={System.Text.Json.JsonSerializer.Serialize(last)}");
    }

    private static int FieldInt(string status, string name)
    {
        var line = status.Split('\n').Single(candidate => candidate.StartsWith(name + "=", StringComparison.Ordinal));
        return int.Parse(line[(name.Length + 1)..], System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string Sha256(byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void WriteSine(string path, int sampleRate, double frequencyHz, double seconds, double amplitude)
    {
        var sampleCount = checked((int)Math.Round(sampleRate * seconds));
        var data = new byte[sampleCount * sizeof(short)];
        for (var index = 0; index < sampleCount; index++)
        {
            var value = Math.Sin(2d * Math.PI * frequencyHz * index / sampleRate) * amplitude;
            var scaled = value < 0 ? value * 32768d : value * 32767d;
            System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(index * sizeof(short)), (short)scaled);
        }

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8);
        writer.Write(36 + data.Length);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * sizeof(short));
        writer.Write((short)sizeof(short));
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(data.Length);
        writer.Write(data);
    }

    private sealed class ProbeSnapshot
    {
        [JsonPropertyName("gumCount")]
        public int GumCount { get; set; }

        [JsonPropertyName("contextCount")]
        public int ContextCount { get; set; }

        [JsonPropertyName("contexts")]
        public List<string> Contexts { get; set; } = [];

        [JsonPropertyName("tracks")]
        public List<ProbeTrack> Tracks { get; set; } = [];

        [JsonPropertyName("enabledChanges")]
        public List<bool> EnabledChanges { get; set; } = [];
    }

    private sealed class ProbeTrack
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("readyState")]
        public string ReadyState { get; set; } = "";
    }
}
