using System.Text;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.RuntimeArtifacts;
using Xunit;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Contracts.Tests.Services;

/// <summary>
/// Prüft Voraussetzungen zwischen Formularen, damit fehlende Referenzen
/// und zyklische Abhängigkeiten im Katalog erkannt werden.
/// </summary>
public sealed class FormsCatalogPreflightMultiFormTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    #region prerequisiteForms Validation Tests

    [Fact]
    public void Validate_MultiFormApp_WithValidPrerequisites_ReturnsOk()
    {
        var catalog = CreateCatalogWithForms(
            ("form-a", null),
            ("form-b", new[] { "form-a" }),
            ("form-c", new[] { "form-a", "form-b" })
        );

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.True(result.Ok, FormatErrors(result));
    }

    [Fact]
    public void Validate_MultiFormApp_WithSelfReference_ReturnsError()
    {
        var catalog = CreateCatalogWithForms(
            ("form-a", new[] { "form-a" })
        );

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Code == PreflightCodes.ErrorPrerequisiteCycle);
    }

    [Fact]
    public void Validate_MultiFormApp_WithDirectCycle_ReturnsError()
    {
        var catalog = CreateCatalogWithForms(
            ("form-a", new[] { "form-b" }),
            ("form-b", new[] { "form-a" })
        );

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Code == PreflightCodes.ErrorPrerequisiteCycle);
    }

    [Fact]
    public void Validate_MultiFormApp_WithIndirectCycle_ReturnsError()
    {
        var catalog = CreateCatalogWithForms(
            ("form-a", new[] { "form-c" }),
            ("form-b", new[] { "form-a" }),
            ("form-c", new[] { "form-b" })
        );

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Code == PreflightCodes.ErrorPrerequisiteCycle);
    }

    [Fact]
    public void Validate_MultiFormApp_WithMissingPrerequisite_ReturnsError()
    {
        var catalog = CreateCatalogWithForms(
            ("form-a", new[] { "nonexistent-form" })
        );

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Code == PreflightCodes.ErrorRefMissing);
    }

    [Fact]
    public void Validate_MultiFormApp_WithNoPrerequisites_ReturnsOk()
    {
        var catalog = CreateCatalogWithForms(
            ("form-a", null),
            ("form-b", null),
            ("form-c", null)
        );

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.True(result.Ok, FormatErrors(result));
    }

    [Fact]
    public void Validate_MultiFormApp_WithDiamondDependency_ReturnsOk()
    {
        var catalog = CreateCatalogWithForms(
            ("form-a", null),
            ("form-b", new[] { "form-a" }),
            ("form-c", new[] { "form-a" }),
            ("form-d", new[] { "form-b", "form-c" })
        );

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.True(result.Ok, FormatErrors(result));
    }

    #endregion

    #region Real Catalog Tests

    [Fact]
    public void Validate_SampleFormsCatalog_ReturnsOk()
    {
        var catalogDocument = JsonSerializer.Deserialize<FormsCatalogDocument>(
            EmbeddedFormsCatalog.ComposedBytes(),
            JsonOptions)
            ?? throw new InvalidOperationException("Could not deserialize composed forms catalog.");
        var selectionDialog = LoadEmbeddedArtifact<ApplicationSelectionDialog>(
            "navigationsmodell/reference-contracts/application-selection-dialogs/application-selection-dialog.default.reference.json");
        var selectionDialogProfile = LoadEmbeddedArtifact<ApplicationSelectionDialogProfile>(
            "navigationsmodell/reference-contracts/application-selection-dialog-profiles/application-selection-dialog-profile.default.reference.json");
        var catalog = FormsCatalogResolver.Resolve(
            catalogDocument,
            selectionDialog,
            new Dictionary<ApplicationSelectionDialogProfileKey, ApplicationSelectionDialogProfile>
            {
                [new ApplicationSelectionDialogProfileKey(selectionDialogProfile.ProfileId, selectionDialogProfile.ProfileVersion)] = selectionDialogProfile
            });

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.True(result.Ok, FormatErrors(result));

        var sampleApp = Assert.Single(catalog.Applications, app => app.ApplicationId == "sample.simple");
        var sampleForm = Assert.Single(sampleApp.Forms);
        Assert.Equal("two-groups", sampleForm.FormId);
        Assert.Null(sampleForm.PrerequisiteForms);

        var complexApp = Assert.Single(catalog.Applications, app => app.ApplicationId == "sample.complex");
        Assert.Equal("full-coverage", Assert.Single(complexApp.Forms).FormId);
    }

    #endregion

    #region Helpers

    private static FormsCatalog CreateCatalogWithForms(params (string FormId, string[]? Prerequisites)[] forms)
    {
        var formDescriptors = forms.Select(f => new FormDescriptor
        {
            FormId = FormId.From(f.FormId),
            DisplayName = new LocalizedText { ["de"] = f.FormId, ["en"] = f.FormId },
            Description = new LocalizedText { ["de"] = "Test", ["en"] = "Test" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"],
            PrerequisiteForms = f.Prerequisites?.ToList(),
            Sequence = null
        }).ToList();

        var app = new ApplicationDescriptor
        {
            ApplicationId = "test.app",
            DisplayName = new LocalizedText { ["de"] = "Test", ["en"] = "Test" },
            Description = new LocalizedText { ["de"] = "Test", ["en"] = "Test" },
            Tags = ["test"],
            SupportedLanguages = ["de", "en"],
            DefaultLanguage = "de",
            Icon = null,
            Forms = formDescriptors
        };

        return new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            GeneratedAt = DateTimeOffset.UtcNow.ToString("o"),
            SelectionDialog = CreateSelectionDialog(["de", "en"]),
            Applications = [app]
        };
    }

    private static ApplicationSelectionDialog CreateSelectionDialog(IReadOnlyList<string> supportedLanguages)
    {
        return new ApplicationSelectionDialog
        {
            SchemaVersion = "1",
            ModelVersion = "1",
            SelectionDialogId = "test.selectionDialog",
            SupportedLanguages = supportedLanguages.ToList(),
            DefaultLanguage = supportedLanguages[0],
            StrictLocalization = true,
            Prompts =
            [
                new CatalogVoicePrompt
                {
                    PromptId = "catalog.app.help",
                    Text = CreateLocalizedText(supportedLanguages, "Help")
                },
                new CatalogVoicePrompt
                {
                    PromptId = ApplicationSelectionRequiredPromptId.AppConfirmationRepeat,
                    Text = CreateLocalizedText(supportedLanguages, "Please say yes or no.")
                },
                new CatalogVoicePrompt
                {
                    PromptId = ApplicationSelectionRequiredPromptId.FormConfirmationRepeat,
                    Text = CreateLocalizedText(supportedLanguages, "Please say yes or no.")
                },
                new CatalogVoicePrompt
                {
                    PromptId = ApplicationSelectionRequiredPromptId.AppReListen,
                    Text = CreateLocalizedText(supportedLanguages, "Got it. I am still listening.")
                },
                new CatalogVoicePrompt
                {
                    PromptId = ApplicationSelectionRequiredPromptId.FormReListen,
                    Text = CreateLocalizedText(supportedLanguages, "Got it. I am still listening.")
                }
            ],
            Intents =
            [
                new CatalogVoiceIntent
                {
                    IntentId = "help",
                    MatchMode = "phrase",
                    Utterances = [CreateLocalizedText(supportedLanguages, "help")]
                }
            ],
            NumberWords =
            [
                new CatalogVoiceNumberWord
                {
                    Value = 1,
                    Utterances = [CreateLocalizedText(supportedLanguages, "one")]
                }
            ]
        };
    }

    private static LocalizedText CreateLocalizedText(IReadOnlyList<string> supportedLanguages, string value)
    {
        var text = new LocalizedText();
        foreach (var language in supportedLanguages)
        {
            text[language] = value;
        }

        return text;
    }

    private static T LoadEmbeddedArtifact<T>(string relativePath) where T : class
    {
        var json = Encoding.UTF8.GetString(MdaEmbeddedRuntimeArtifacts.ReadAllBytes(relativePath));
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Could not deserialize embedded artifact '{relativePath}'.");
    }

    private static string FormatErrors(PreflightResult result)
    {
        if (result.Ok)
        {
            return "No errors";
        }

        var sb = new StringBuilder();
        sb.AppendLine("Preflight errors:");
        foreach (var error in result.Errors)
        {
            sb.AppendLine($"  [{error.Code}] {error.Message} at {error.Path}");
        }

        return sb.ToString();
    }

    #endregion
}
