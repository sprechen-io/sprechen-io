using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.RuntimeArtifacts;
using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Speech.ModelRuntime.Services;

/// <summary>
/// Lädt eingebettete Dialogmodellartefakte, prüft ihre Einzelverträge und
/// Verknüpfungen und löst sie zu einem validierten Laufzeitmodell auf.
/// </summary>
internal static class VoiceInterfaceModelLoader
{
    private const string DocumentPath = "dialogmodell/reference-contracts/voice-interface/voice-interface.reference.json";
    private const string ResourceCatalogPath = "dialogmodell/reference-contracts/voice-interface/voice-resource-catalog.reference.json";
    private const string PromptCatalogPath = "dialogmodell/reference-contracts/voice-interface/generic-prompt-catalog.reference.json";
    private const string LexiconPath = "dialogmodell/reference-contracts/voice-interface/voice-lexicon-catalog.reference.json";
    private const string WakeWordCatalogPath = "dialogmodell/reference-contracts/voice-interface/wake-word-catalog.reference.json";
    private const string WakeWordDetectorCatalogPath = "dialogmodell/reference-contracts/voice-interface/wake-word-detector-catalog.reference.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static VoiceInterface Load()
    {
        var document = LoadResource<VoiceInterfaceDocument>(DocumentPath);
        var resourceCatalog = LoadResource<VoiceResourceCatalog>(ResourceCatalogPath);
        var promptCatalog = LoadResource<GenericPromptCatalog>(PromptCatalogPath);
        var lexiconCatalog = LoadResource<VoiceLexiconCatalog>(LexiconPath);
        var wakeWordCatalog = LoadResource<WakeWordCatalog>(WakeWordCatalogPath);
        var wakeWordDetectorCatalog = LoadResource<WakeWordDetectorCatalog>(WakeWordDetectorCatalogPath);

        ValidateResourceCatalog(resourceCatalog);
        ValidatePromptCatalog(promptCatalog);
        ValidateLexiconCatalog(lexiconCatalog);
        ValidateWakeWordCatalog(wakeWordCatalog);
        ValidateWakeWordDetectorCatalog(wakeWordDetectorCatalog);
        ValidateDocument(document, resourceCatalog, promptCatalog, lexiconCatalog, wakeWordCatalog, wakeWordDetectorCatalog);

        var model = VoiceInterfaceResolver.Resolve(document, resourceCatalog, promptCatalog, lexiconCatalog, wakeWordCatalog, wakeWordDetectorCatalog);
        ValidateModel(model);
        return model;
    }

    private static T LoadResource<T>(string relativePath) where T : class
    {
        var json = MdaEmbeddedRuntimeArtifacts.ReadAllBytes(relativePath);
        return JsonSerializer.Deserialize<T>(json, SerializerOptions)
            ?? throw new InvalidOperationException($"Embedded voice-interface resource '{relativePath}' could not be deserialized.");
    }

    private static void ValidateDocument(
        VoiceInterfaceDocument document,
        VoiceResourceCatalog resourceCatalog,
        GenericPromptCatalog promptCatalog,
        VoiceLexiconCatalog lexiconCatalog,
        WakeWordCatalog wakeWordCatalog,
        WakeWordDetectorCatalog wakeWordDetectorCatalog)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(resourceCatalog);
        ArgumentNullException.ThrowIfNull(promptCatalog);
        ArgumentNullException.ThrowIfNull(lexiconCatalog);
        ArgumentNullException.ThrowIfNull(wakeWordCatalog);
        ArgumentNullException.ThrowIfNull(wakeWordDetectorCatalog);

        var preflight = VoiceInterfaceDocumentPreflight.Validate(document, resourceCatalog, promptCatalog, lexiconCatalog, wakeWordCatalog, wakeWordDetectorCatalog);
        if (!preflight.Ok)
        {
            throw new InvalidOperationException($"Embedded voice-interface document preflight failed: {FormatIssues(preflight.Errors)}");
        }
    }

    private static void ValidateResourceCatalog(VoiceResourceCatalog resourceCatalog)
    {
        ArgumentNullException.ThrowIfNull(resourceCatalog);

        var preflight = VoiceResourceCatalogPreflight.Validate(resourceCatalog);
        if (!preflight.Ok)
        {
            throw new InvalidOperationException($"Embedded voice-resource catalog preflight failed: {FormatIssues(preflight.Errors)}");
        }
    }

    private static void ValidateLexiconCatalog(VoiceLexiconCatalog lexiconCatalog)
    {
        ArgumentNullException.ThrowIfNull(lexiconCatalog);

        var preflight = VoiceLexiconCatalogPreflight.Validate(lexiconCatalog);
        if (!preflight.Ok)
        {
            throw new InvalidOperationException($"Embedded voice-lexicon catalog preflight failed: {FormatIssues(preflight.Errors)}");
        }
    }

    private static void ValidatePromptCatalog(GenericPromptCatalog promptCatalog)
    {
        ArgumentNullException.ThrowIfNull(promptCatalog);

        var preflight = GenericPromptCatalogPreflight.Validate(promptCatalog);
        if (!preflight.Ok)
        {
            throw new InvalidOperationException($"Embedded generic-prompt catalog preflight failed: {FormatIssues(preflight.Errors)}");
        }
    }

    private static void ValidateWakeWordCatalog(WakeWordCatalog wakeWordCatalog)
    {
        ArgumentNullException.ThrowIfNull(wakeWordCatalog);

        var preflight = WakeWordCatalogPreflight.Validate(wakeWordCatalog);
        if (!preflight.Ok)
        {
            throw new InvalidOperationException($"Embedded wake-word catalog preflight failed: {FormatIssues(preflight.Errors)}");
        }
    }

    private static void ValidateWakeWordDetectorCatalog(WakeWordDetectorCatalog wakeWordDetectorCatalog)
    {
        ArgumentNullException.ThrowIfNull(wakeWordDetectorCatalog);

        var preflight = WakeWordDetectorCatalogPreflight.Validate(wakeWordDetectorCatalog);
        if (!preflight.Ok)
        {
            throw new InvalidOperationException($"Embedded wake-word detector catalog preflight failed: {FormatIssues(preflight.Errors)}");
        }
    }

    private static void ValidateModel(VoiceInterface model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var preflight = VoiceInterfacePreflight.Validate(model);
        if (!preflight.Ok)
        {
            throw new InvalidOperationException($"Embedded voice-interface model preflight failed: {FormatIssues(preflight.Errors)}");
        }
    }

    private static string FormatIssues(IEnumerable<PreflightIssue> issues)
    {
        return string.Join(
            " | ",
            issues.Select(issue =>
            {
                var suffix = string.IsNullOrWhiteSpace(issue.Path)
                    ? string.Empty
                    : $" @ {issue.Path}";
                return $"{issue.Code}:{issue.Message}{suffix}";
            }));
    }
}
