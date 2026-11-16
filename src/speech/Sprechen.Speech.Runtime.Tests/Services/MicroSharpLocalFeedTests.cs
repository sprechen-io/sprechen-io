using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Prüft, dass MicroSharp aus dem lokalen Paketfeed stammt und die wiederhergestellten
/// Bytes mit dem gepackten Inhalt übereinstimmen.
/// </summary>
public sealed class MicroSharpLocalFeedTests
{
    [Fact]
    public void RestoredBrowserPackageMatchesTheLocalFeedAndIsNotAProject()
    {
        var root = FindRepositoryRoot();
        var assetsPath = Path.Combine(
            root,
            "src",
            "speech",
            "Sprechen.Speech.App.MicrophoneRecording",
            "obj",
            "project.assets.json");
        using var assets = JsonDocument.Parse(File.ReadAllText(assetsPath));
        var rootElement = assets.RootElement;
        var library = rootElement.GetProperty("libraries").GetProperty("MicroSharp.Browser/0.1.0");
        Assert.Equal("package", library.GetProperty("type").GetString());
        Assert.Equal("microsharp.browser/0.1.0", library.GetProperty("path").GetString());
        var core = rootElement.GetProperty("libraries").GetProperty("MicroSharp/0.1.0");
        Assert.Equal("package", core.GetProperty("type").GetString());

        foreach (var project in rootElement.GetProperty("libraries").EnumerateObject())
        {
            if (project.Name.StartsWith("MicroSharp", StringComparison.Ordinal))
            {
                Assert.Equal("package", project.Value.GetProperty("type").GetString());
                Assert.DoesNotContain("ProjectReference", project.Value.ToString(), StringComparison.Ordinal);
            }
        }

        var packageFolder = rootElement.GetProperty("packageFolders").EnumerateObject().Single().Name;
        var restoredDirectory = Path.Combine(packageFolder, "microsharp.browser", "0.1.0");
        var nupkgPath = Path.Combine(root, "local-feed", "MicroSharp.Browser.0.1.0.nupkg");
        using var nupkg = ZipFile.OpenRead(nupkgPath);
        AssertSameEntry(nupkg, "lib/net11.0/MicroSharp.Browser.dll", Path.Combine(restoredDirectory, "lib", "net11.0", "MicroSharp.Browser.dll"));
        AssertSameEntry(
            nupkg,
            "staticwebassets/microsharp-capture.js",
            Path.Combine(restoredDirectory, "staticwebassets", "microsharp-capture.js"));
    }

    private static void AssertSameEntry(ZipArchive nupkg, string entryName, string restoredPath)
    {
        var entry = nupkg.GetEntry(entryName);
        Assert.NotNull(entry);
        using var entryStream = entry.Open();
        var packed = SHA256.HashData(entryStream);
        var restored = SHA256.HashData(File.ReadAllBytes(restoredPath));
        Assert.Equal(Convert.ToHexString(packed), Convert.ToHexString(restored));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var config = Path.Combine(directory.FullName, "nuget.config");
            var feed = Path.Combine(directory.FullName, "local-feed");
            if (File.Exists(config) && Directory.Exists(feed))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("The local MicroSharp feed is not beside nuget.config.");
    }
}
