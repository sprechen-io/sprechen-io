namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Product check that the speech application consumes MicroSharp and does not keep a second capture implementation.
/// </summary>
public sealed class MicrophoneConsumerBoundaryTests
{
    [Fact]
    public void ProductionMicrophoneProjectConsumesThePackage()
    {
        var root = FindRepositoryRoot();
        var recording = Path.Combine(root, "src", "speech", "Sprechen.Speech.App.MicrophoneRecording");
        var violations = new List<string>();
        foreach (var file in Directory.EnumerateFiles(recording, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            foreach (var token in new[]
            {
                "MicrophoneJsTestBridge",
                "CreateLegacy",
                "JSInvokable",
                "Channel.CreateUnbounded",
                "OnBrowserSessionEnded",
                "MicrophoneAcquisitionDto",
                "MicrophoneTrackProcessingDto",
            })
            {
                if (text.Contains(token, StringComparison.Ordinal))
                {
                    violations.Add($"{Path.GetRelativePath(root, file)}:{token}");
                }
            }
        }

        Assert.Empty(violations);
        Assert.False(Directory.Exists(Path.Combine(recording, "TypeScript")));
        Assert.False(File.Exists(Path.Combine(recording, "package.json")));
        var project = File.ReadAllText(Path.Combine(recording, "Sprechen.Speech.App.MicrophoneRecording.csproj"));
        Assert.Contains("PackageReference Include=\"MicroSharp.Browser\"", project, StringComparison.Ordinal);
        Assert.DoesNotContain("MicroSharp.Browser.csproj", project, StringComparison.Ordinal);
        Assert.DoesNotContain("MicroSharp.csproj", project, StringComparison.Ordinal);

        var analyzer = File.ReadAllText(Path.Combine(
            root,
            "src",
            "speech",
            "Sprechen.Speech.App.CaptureRuntime",
            "Services",
            "PcmAudioChunkAnalyzer.cs"));
        Assert.Contains("Pcm16LeBlockAnalyzer", analyzer, StringComparison.Ordinal);
        Assert.DoesNotContain("Math.Abs", analyzer, StringComparison.Ordinal);
        Assert.DoesNotContain("% 1000", analyzer, StringComparison.Ordinal);

        var ring = File.ReadAllText(Path.Combine(
            root,
            "src",
            "speech",
            "Sprechen.Speech.App.CaptureRuntime",
            "Services",
            "InMemorySpeechSampleRing.cs"));
        Assert.Contains("new PcmHistoryRing", ring, StringComparison.Ordinal);
        Assert.DoesNotContain("SampleRateHz * BytesPerSample", ring, StringComparison.Ordinal);

        var reader = File.ReadAllText(Path.Combine(
            root,
            "src",
            "speech",
            "Sprechen.Speech.App.CaptureRuntime",
            "Services",
            "PcmRingChunkReader.cs"));
        Assert.DoesNotContain("ArrayPool", reader, StringComparison.Ordinal);
        Assert.Contains("ReadRightSized", reader, StringComparison.Ordinal);

        var schema = File.ReadAllText(Path.Combine(
            root,
            "src",
            "model",
            "dialogmodell",
            "schemas",
            "speech-capture-profile.schema.json"));
        Assert.DoesNotContain("microphoneRecorder.ts", schema, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "sprechen-io.slnx"))
                || File.Exists(Path.Combine(directory.FullName, "Sprechen.sln"))
                || Directory.Exists(Path.Combine(directory.FullName, "src", "speech", "Sprechen.Speech.App.MicrophoneRecording")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("The application repository root was not found.");
    }
}
