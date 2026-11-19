using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Checks the MicroSharp package references and their restored payloads.
/// </summary>
public sealed class MicroSharpPackageRestoreTests
{
    [Fact]
    public void RestoredBrowserPackageContainsTheExpectedPayloadAndIsNotAProject()
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
        var library = rootElement.GetProperty("libraries").GetProperty("MicroSharp.Browser/0.3.0-alpha.1");
        Assert.Equal("package", library.GetProperty("type").GetString());
        Assert.Equal("microsharp.browser/0.3.0-alpha.1", library.GetProperty("path").GetString());
        var core = rootElement.GetProperty("libraries").GetProperty("MicroSharp/0.3.0-alpha.1");
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
        var restoredDirectory = Path.Combine(packageFolder, "microsharp.browser", "0.3.0-alpha.1");
        var nupkgPath = Path.Combine(restoredDirectory, "microsharp.browser.0.3.0-alpha.1.nupkg");
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
            if (File.Exists(config) && Directory.Exists(Path.Combine(directory.FullName, "src", "speech")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("The application repository containing nuget.config was not found.");
    }
}
