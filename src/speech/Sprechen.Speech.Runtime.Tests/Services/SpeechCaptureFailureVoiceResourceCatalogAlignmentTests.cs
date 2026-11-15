using System.Text.Json;
using Sprechen.Mda.Contracts.V1.RuntimeArtifacts;
using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechCaptureFailureVoiceResourceCatalogAlignmentTests
{
    private const string VoiceResourceCatalogPath =
        "dialogmodell/reference-contracts/voice-interface/voice-resource-catalog.reference.json";

    [Fact]
    public void EmbeddedVoiceResourceCatalog_ContainsExactlyTheModeledSpeechCaptureFailureResourceIds()
    {
        var expectedIds = SpeechCaptureFailureResourceIds()
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        var actualIds = LoadCatalogDocument()
            .RootElement
            .GetProperty("resources")
            .EnumerateArray()
            .Select(resource => resource.GetProperty("id").GetString())
            .Where(id => id?.StartsWith("speechCapture.failure.", StringComparison.Ordinal) == true)
            .Select(id => id!)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedIds, actualIds);
    }

    [Fact]
    public void EmbeddedVoiceResourceCatalog_ContainsNonEmptyLocalizedTextsForModeledSpeechCaptureFailures()
    {
        foreach (var resourceId in SpeechCaptureFailureResourceIds())
        {
            var localizedTexts = LoadLocalizedTexts(resourceId);

            foreach (var language in VoiceInterfaceModelCatalog.SupportedLanguages)
            {
                Assert.True(
                    localizedTexts.TryGetValue(language, out var text),
                    $"Resource '{resourceId}' is missing language '{language}'.");
                Assert.False(
                    string.IsNullOrWhiteSpace(text),
                    $"Resource '{resourceId}' has empty text for language '{language}'.");
            }
        }
    }

    private static IReadOnlyList<string> SpeechCaptureFailureResourceIds() =>
    [
        VoiceInterfaceResourceIds.SpeechCaptureFailureVoiceCommandDialogFailed,
        VoiceInterfaceResourceIds.SpeechCaptureFailureDefault,
        VoiceInterfaceResourceIds.SpeechCaptureFailurePermissionDenied,
        VoiceInterfaceResourceIds.SpeechCaptureFailureMicrophoneUnavailable,
        VoiceInterfaceResourceIds.SpeechCaptureFailureGeneric,
        VoiceInterfaceResourceIds.SpeechCaptureFailureRuleLoopGuardExceeded
    ];

    private static IReadOnlyDictionary<string, string> LoadLocalizedTexts(string resourceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);

        using var document = LoadCatalogDocument();
        foreach (var resource in document.RootElement.GetProperty("resources").EnumerateArray())
        {
            var currentId = resource.GetProperty("id").GetString();
            if (!string.Equals(currentId, resourceId, StringComparison.Ordinal))
            {
                continue;
            }

            return resource.GetProperty("texts")
                .EnumerateObject()
                .ToDictionary(
                    property => property.Name,
                    property => property.Value.GetString() ?? string.Empty,
                    StringComparer.Ordinal);
        }

        throw new InvalidOperationException($"Resource '{resourceId}' was not found in the embedded voice resource catalog.");
    }

    private static JsonDocument LoadCatalogDocument()
    {
        var json = MdaEmbeddedRuntimeArtifacts.ReadAllBytes(VoiceResourceCatalogPath);
        return JsonDocument.Parse(json);
    }
}
