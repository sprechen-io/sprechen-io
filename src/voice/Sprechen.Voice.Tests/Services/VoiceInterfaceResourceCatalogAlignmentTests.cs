using System.Text.Json;
using Sprechen.Mda.Contracts.V1.RuntimeArtifacts;
using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;
using Sprechen.Speech.App.Contracts.Services;
using Xunit;

namespace Sprechen.Voice.Tests.Services;

public sealed class VoiceInterfaceResourceCatalogAlignmentTests
{
    private const string VoiceResourceCatalogPath =
        "dialogmodell/reference-contracts/voice-interface/voice-resource-catalog.reference.json";

    [Fact]
    public void EmbeddedVoiceResourceCatalog_ContainsAllVoiceShellResourceIds()
    {
        var resourceIds = LoadResourceIds();

        foreach (var resourceId in CanonicalVoiceShellResourceIds())
        {
            Assert.Contains(resourceId, resourceIds);
        }
    }

    [Fact]
    public void EmbeddedVoiceResourceCatalog_ContainsNonEmptyLocalizedTextsForAllRequiredVoiceShellResources()
    {
        foreach (var resourceId in CanonicalVoiceShellResourceIds())
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

    [Fact]
    public void EmbeddedVoiceResourceCatalog_ContainsWakeWordSetupPromptWithActivationPlaceholdersOnly()
    {
        var localizedTexts = LoadLocalizedTexts(VoiceInterfaceResourceIds.SessionSetupWakeWordsPrompt);

        foreach (var text in localizedTexts.Values)
        {
            Assert.Contains("{wakeWords}", text, StringComparison.Ordinal);
            Assert.Contains("{yes}", text, StringComparison.Ordinal);
            Assert.Contains("{no}", text, StringComparison.Ordinal);
            Assert.DoesNotContain("{requiredPrefix}", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void EmbeddedVoiceResourceCatalog_ContainsReadyWithWakeWordsResourceWithWakeWordsPlaceholderForAllLanguages()
    {
        var localizedTexts = LoadLocalizedTexts(VoiceInterfaceResourceIds.SpeechCaptureStatusReadyWithWakeWords);

        foreach (var language in VoiceInterfaceModelCatalog.SupportedLanguages)
        {
            Assert.True(localizedTexts.TryGetValue(language, out var text), $"Missing language '{language}'.");
            Assert.Contains("{wakeWords}", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void EmbeddedVoiceResourceCatalog_ContainsLanguageNameResourcesForAllSupportedLanguages()
    {
        var resolver = new VoiceLanguageNameResourceIdResolver();
        foreach (var language in VoiceInterfaceModelCatalog.SupportedLanguages)
        {
            var localizedTexts = LoadLocalizedTexts(resolver.Resolve(language));
            foreach (var text in localizedTexts.Values)
            {
                Assert.False(string.IsNullOrWhiteSpace(text));
            }
        }
    }

    [Fact]
    public void EmbeddedVoiceResourceCatalog_ContainsFormSurfaceSummaryTemplatesWithRequiredPlaceholders()
    {
        var languageSummaryTexts = LoadLocalizedTexts(VoiceInterfaceResourceIds.FormSurfaceLanguageSummary);

        foreach (var text in languageSummaryTexts.Values)
        {
            Assert.Contains("{languageName}", text, StringComparison.Ordinal);
        }

        var focusedFieldTexts = LoadLocalizedTexts(VoiceInterfaceResourceIds.FormSurfaceFocusedFieldSummary);

        foreach (var text in focusedFieldTexts.Values)
        {
            Assert.Contains("{fieldLabel}", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void EmbeddedVoiceResourceCatalog_ContainsStartupFailureResourcesForAllLanguages()
    {
        string[] startupFailureResourceIds =
        [
            VoiceInterfaceResourceIds.SpeechCaptureFailurePermissionDenied,
            VoiceInterfaceResourceIds.SpeechCaptureFailureMicrophoneUnavailable,
            VoiceInterfaceResourceIds.SpeechCaptureFailureGeneric
        ];

        foreach (var resourceId in startupFailureResourceIds)
        {
            var localizedTexts = LoadLocalizedTexts(resourceId);
            foreach (var language in VoiceInterfaceModelCatalog.SupportedLanguages)
            {
                Assert.True(localizedTexts.TryGetValue(language, out var text), $"Resource '{resourceId}' is missing language '{language}'.");
                Assert.False(string.IsNullOrWhiteSpace(text), $"Resource '{resourceId}' has empty text for language '{language}'.");
            }
        }
    }

    [Fact]
    public void EmbeddedVoiceResourceCatalog_GermanPermissionDeniedFailureIncludesSelfHelpRecoveryHint()
    {
        var localizedTexts = LoadLocalizedTexts(VoiceInterfaceResourceIds.SpeechCaptureFailurePermissionDenied);

        Assert.True(localizedTexts.TryGetValue("de", out var german));
        Assert.Contains("Browsereinstellungen", german, StringComparison.Ordinal);
        Assert.Contains("erlauben", german, StringComparison.Ordinal);
    }

    [Fact]
    public void EmbeddedVoiceResourceCatalog_GenericStartupFailureNeverClaimsListeningHasResumed()
    {
        // Nach einem fehlgeschlagenen Start darf der Text keine erneute Zuhörbereitschaft versprechen.
        // Hier werden dafür ausgewählte Wörter ausgeschlossen und der allgemeine Fehlertext abgegrenzt.
        var genericTexts = LoadLocalizedTexts(VoiceInterfaceResourceIds.SpeechCaptureFailureGeneric);
        var defaultTexts = LoadLocalizedTexts(VoiceInterfaceResourceIds.SpeechCaptureFailureDefault);
        IReadOnlyDictionary<string, string> listeningClaims = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["de"] = "Zuhörbereitschaft",
            ["en"] = "listening",
            ["fr"] = "écoute",
            ["es"] = "escuchar",
            ["it"] = "ascolto"
        };

        foreach (var language in VoiceInterfaceModelCatalog.SupportedLanguages)
        {
            Assert.NotEqual(defaultTexts[language], genericTexts[language]);
            Assert.DoesNotContain(
                listeningClaims[language],
                genericTexts[language],
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void VoiceShellResourceInventory_PartitionsAreExactAndPairwiseDisjoint()
    {

        string[] expectedActive =
        [
            "sessionSetup.earconsIntro",
            "sessionSetup.earcon.flowListening",
            "sessionSetup.earcon.flowProcessing",
            "sessionSetup.earcon.validationSuccess",
            "sessionSetup.earcon.voiceInputMicOff",
            "sessionSetup.earcon.voiceInputMicOn",
            "sessionSetup.wakeWords.prompt",
            "sessionSetup.wakeWords.askNew",
            "sessionSetup.wakeWords.notRecognized",
            "sessionSetup.wakeWords.confirm",
            "sessionSetup.wakeWords.saved",
            "settings.loading",
            "settings.sessionLanguage.summary",
            "settings.enabled",
            "settings.disabled",
            "settings.audio.volumeTest",
        ];
        string[] expectedCatalogOnly =
        [
            "sessionSetup.earcon.structureStepTransition",
            "sessionSetup.earcon.structureBoundary",
        ];
        // Ressourcen ohne zugeordneten Wiedergabeaufrufer gehören nicht zur aktiven Earcon-Einführung.

        string[] expectedProducerless =
        [
            "sessionSetup.earcon.validationNotOk",
            "sessionSetup.earcon.modePrivacyChange",
            "sessionSetup.earcon.confirmationRequired",
        ];


        Assert.Equal(expectedActive.ToHashSet(StringComparer.Ordinal), VoiceInterfaceResourceIds.ActiveVoiceShellResourceIds.ToHashSet(StringComparer.Ordinal));
        Assert.Equal(expectedCatalogOnly.ToHashSet(StringComparer.Ordinal), VoiceInterfaceResourceIds.CatalogOnlyStructureResourceIds.ToHashSet(StringComparer.Ordinal));
        Assert.Equal(expectedProducerless.ToHashSet(StringComparer.Ordinal), VoiceInterfaceResourceIds.ProducerlessEarconResourceIds.ToHashSet(StringComparer.Ordinal));


        var active = VoiceInterfaceResourceIds.ActiveVoiceShellResourceIds;
        var catalogOnly = VoiceInterfaceResourceIds.CatalogOnlyStructureResourceIds;
        var producerless = VoiceInterfaceResourceIds.ProducerlessEarconResourceIds;
        Assert.Empty(active.Intersect(catalogOnly, StringComparer.Ordinal));
        Assert.Empty(active.Intersect(producerless, StringComparer.Ordinal));
        Assert.Empty(catalogOnly.Intersect(producerless, StringComparer.Ordinal));


        var catalogIds = LoadResourceIds();
        foreach (var resourceId in active.Concat(catalogOnly).Concat(producerless))
        {
            Assert.Contains(resourceId, catalogIds);
        }
    }

    [Fact]
    public void EmbeddedVoiceResourceCatalog_MatchesDeclaredAndGeneratedResourceIds_Bidirectional()
    {
        // Zu den deklarierten Konstanten kommen die erzeugten Kennungen für die Sprachnamen.

        var declaredIds = typeof(VoiceInterfaceResourceIds)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .Select(field => (string)field.GetValue(null)!)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Equal(138, declaredIds.Count);

        var resolver = new VoiceLanguageNameResourceIdResolver();
        var expected = new HashSet<string>(declaredIds, StringComparer.Ordinal);
        foreach (var language in VoiceInterfaceModelCatalog.SupportedLanguages)
        {
            Assert.True(expected.Add(resolver.Resolve(language)), $"Duplicate generated language-name ID for '{language}'.");
        }


        var catalogIds = LoadResourceIds();
        var missingInCatalog = expected.Except(catalogIds, StringComparer.Ordinal).OrderBy(id => id).ToArray();
        var unexpectedInCatalog = catalogIds.Except(expected, StringComparer.Ordinal).OrderBy(id => id).ToArray();
        Assert.True(
            missingInCatalog.Length == 0 && unexpectedInCatalog.Length == 0,
            "Catalog/declaration drift."
            + $" Missing in catalog: [{string.Join(", ", missingInCatalog)}]."
            + $" Unexpected in catalog: [{string.Join(", ", unexpectedInCatalog)}].");
    }

    private static HashSet<string> LoadResourceIds()
    {
        using var document = LoadCatalogDocument();

        return document.RootElement
            .GetProperty("resources")
            .EnumerateArray()
            .Select(resource => resource.GetProperty("id").GetString())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .ToHashSet(StringComparer.Ordinal);
    }

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

            var textsElement = resource.GetProperty("texts");
            return textsElement.EnumerateObject()
                .ToDictionary(
                    property => property.Name,
                    property => property.Value.GetString() ?? string.Empty,
                    StringComparer.Ordinal);
        }

        throw new InvalidOperationException($"Resource '{resourceId}' was not found in the embedded voice resource catalog.");
    }

    private static IReadOnlyList<string> CanonicalVoiceShellResourceIds() =>
        VoiceInterfaceResourceIds.ActiveVoiceShellResourceIds
            .Concat(VoiceInterfaceResourceIds.CatalogOnlyStructureResourceIds)
            .Concat(VoiceInterfaceResourceIds.ProducerlessEarconResourceIds)
            .ToArray();

    private static JsonDocument LoadCatalogDocument()
    {
        var json = MdaEmbeddedRuntimeArtifacts.ReadAllBytes(VoiceResourceCatalogPath);
        return JsonDocument.Parse(json);
    }
}
