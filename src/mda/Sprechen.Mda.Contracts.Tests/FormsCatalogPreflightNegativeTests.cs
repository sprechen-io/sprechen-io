using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Preflight;
using Xunit;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.Tests;

public sealed class FormsCatalogPreflightNegativeTests
{
    [Fact]
    public void Validate_WhenDuplicateApplicationId_ReturnsError()
    {
        var catalog = CreateValidCatalog();
        catalog.Applications.Add(new ApplicationDescriptor
        {
            ApplicationId = "app1",
            DisplayName = new LocalizedText { ["de"] = "Dupe" },
            Description = new LocalizedText { ["de"] = "Dupe" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms =
            [
                new FormDescriptor
                {
                    FormId = FormId.From("form1"),
                    DisplayName = new LocalizedText { ["de"] = "Form" },
                    Description = new LocalizedText { ["de"] = "Desc" },
                    LatestContractVersion = "1",
                    AvailableContractVersions = ["1"]
                }
            ]
        });

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Code == PreflightCodes.ErrorIdDuplicate && e.Id == "app1");
    }

    [Fact]
    public void Validate_WhenDuplicateApplicationIdDiffersOnlyByCase_ReturnsError()
    {
        var catalog = CreateValidCatalog();
        catalog.Applications.Add(new ApplicationDescriptor
        {
            ApplicationId = "APP1",
            DisplayName = new LocalizedText { ["de"] = "Dupe" },
            Description = new LocalizedText { ["de"] = "Dupe" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms =
            [
                new FormDescriptor
                {
                    FormId = FormId.From("form2"),
                    DisplayName = new LocalizedText { ["de"] = "Form" },
                    Description = new LocalizedText { ["de"] = "Desc" },
                    LatestContractVersion = "1",
                    AvailableContractVersions = ["1"]
                }
            ]
        });

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Code == PreflightCodes.ErrorIdDuplicate && e.Id == "APP1");
    }

    [Fact]
    public void Validate_WhenDefaultLanguageNotInSupported_ReturnsError()
    {
        var catalog = new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "app1",
                    DisplayName = new LocalizedText { ["de"] = "App", ["en"] = "App" },
                    Description = new LocalizedText { ["de"] = "Desc", ["en"] = "Desc" },
                    SupportedLanguages = ["de", "en"],
                    DefaultLanguage = "fr",
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("form1"),
                            DisplayName = new LocalizedText { ["de"] = "Form", ["en"] = "Form" },
                            Description = new LocalizedText { ["de"] = "Desc", ["en"] = "Desc" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        }
                    ]
                }
            ]
        };

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Code == PreflightCodes.ErrorLanguageInvariantBroken);
    }

    [Fact]
    public void Validate_WhenLocalizationMissing_ReturnsError()
    {
        var catalog = new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "app1",
                    DisplayName = new LocalizedText { ["de"] = "App" },
                    Description = new LocalizedText { ["de"] = "Desc" },
                    SupportedLanguages = ["de", "en"],
                    DefaultLanguage = "de",
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("form1"),
                            DisplayName = new LocalizedText { ["de"] = "Form" },
                            Description = new LocalizedText { ["de"] = "Desc" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        }
                    ]
                }
            ]
        };

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Code == PreflightCodes.ErrorLocalizationMissing);
        Assert.DoesNotContain(result.Errors, e => IsSelectionNameError(e.Code));
    }

    [Fact]
    public void Validate_WhenLatestNotInAvailable_ReturnsError()
    {
        var catalog = new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "app1",
                    DisplayName = new LocalizedText { ["de"] = "App" },
                    Description = new LocalizedText { ["de"] = "Desc" },
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("form1"),
                            DisplayName = new LocalizedText { ["de"] = "Form" },
                            Description = new LocalizedText { ["de"] = "Desc" },
                            LatestContractVersion = "2",
                            AvailableContractVersions = ["1"]
                        }
                    ]
                }
            ]
        };

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Code == PreflightCodes.ErrorVersionInvariantBroken);
    }

    [Fact]
    public void Validate_WhenPrerequisiteFormMissing_ReturnsError()
    {
        var catalog = new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "app1",
                    DisplayName = new LocalizedText { ["de"] = "App" },
                    Description = new LocalizedText { ["de"] = "Desc" },
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("form1"),
                            DisplayName = new LocalizedText { ["de"] = "Form" },
                            Description = new LocalizedText { ["de"] = "Desc" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"],
                            PrerequisiteForms = ["nonexistent"]
                        }
                    ]
                }
            ]
        };

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e => e.Code == PreflightCodes.ErrorRefMissing && e.Id == "nonexistent");
    }

    [Fact]
    public void Validate_WhenPrerequisiteCycleExists_ReturnsError()
    {
        var catalog = new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "app1",
                    DisplayName = new LocalizedText { ["de"] = "App" },
                    Description = new LocalizedText { ["de"] = "Desc" },
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("formA"),
                            DisplayName = new LocalizedText { ["de"] = "Form A" },
                            Description = new LocalizedText { ["de"] = "Desc" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"],
                            PrerequisiteForms = ["formB"]
                        },
                        new FormDescriptor
                        {
                            FormId = FormId.From("formB"),
                            DisplayName = new LocalizedText { ["de"] = "Form B" },
                            Description = new LocalizedText { ["de"] = "Desc" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"],
                            PrerequisiteForms = ["formA"]
                        }
                    ]
                }
            ]
        };

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e =>
            e.Code == PreflightCodes.ErrorPrerequisiteCycle &&
            e.Path == "$.applications[0].forms" &&
            e.Id == "formA");
    }

    [Fact]
    public void Validate_WhenVoiceDialogSettingsInvalid_ReturnsError()
    {
        var catalog = CreateValidCatalog();
        catalog.VoiceDialogSettings = new VoiceDialogSettings
        {
            NoInputTimeoutMs = 100,
            MaxRetries = 100,
            SpeechRate = 5.0,
            PauseBetweenItemsMs = 10000,
            CancelRefractoryMs = 10001,
            PolitenessLevel = (PolitenessLevel)123,
            ProgressCalculationBasis = (ProgressCalculationBasis)123
        };

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.False(result.Ok);
        Assert.Equal(7, result.Errors.Count);
        Assert.All(result.Errors, e => Assert.Equal(PreflightCodes.ErrorSettingsInvalid, e.Code));
        Assert.All(result.Errors, e => Assert.Equal("catalog", e.Id));

        Assert.Contains(result.Errors, e => e.Path == "$.voiceDialogSettings.noInputTimeoutMs");
        Assert.Contains(result.Errors, e => e.Path == "$.voiceDialogSettings.maxRetries");
        Assert.Contains(result.Errors, e => e.Path == "$.voiceDialogSettings.speechRate");
        Assert.Contains(result.Errors, e => e.Path == "$.voiceDialogSettings.pauseBetweenItemsMs");
        Assert.Contains(result.Errors, e => e.Path == "$.voiceDialogSettings.cancelRefractoryMs");
        Assert.Contains(result.Errors, e => e.Path == "$.voiceDialogSettings.politenessLevel");
        Assert.Contains(result.Errors, e => e.Path == "$.voiceDialogSettings.progressCalculationBasis");
    }

    [Fact]
    public void Validate_WhenApplicationVoiceDialogSettingsInvalid_ReturnsError()
    {
        var catalog = CreateValidCatalog();
        catalog.Applications[0].VoiceDialogSettings = new VoiceDialogSettings
        {
            NoInputTimeoutMs = 100
        };

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, e =>
            e.Code == PreflightCodes.ErrorSettingsInvalid &&
            e.Id == "app1" &&
            e.Path == "$.applications[0].voiceDialogSettings.noInputTimeoutMs");
    }

    [Theory]
    [InlineData(-1, null, "sttMinConfidenceToAccept")]
    [InlineData(101, null, "sttMinConfidenceToAccept")]
    [InlineData(null, -1, "sttMinConfidenceToConfirm")]
    [InlineData(null, 101, "sttMinConfidenceToConfirm")]
    public void Validate_WhenSelectionConfidenceThresholdOutsidePercentRange_ReturnsError(
        int? acceptThreshold,
        int? confirmThreshold,
        string invalidProperty)
    {
        var catalog = CreateValidCatalog();
        catalog.VoiceDialogSettings = new VoiceDialogSettings
        {
            SttMinConfidenceToAccept = acceptThreshold,
            SttMinConfidenceToConfirm = confirmThreshold
        };

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, error =>
            error.Code == PreflightCodes.ErrorSettingsInvalid
            && error.Id == "catalog"
            && error.Path == $"$.voiceDialogSettings.{invalidProperty}");
    }

    [Theory]
    [InlineData(64, 65)]
    [InlineData(50, null)]
    [InlineData(null, 90)]
    public void Validate_WhenEffectiveSelectionConfidenceThresholdsAreInverted_ReturnsError(
        int? acceptThreshold,
        int? confirmThreshold)
    {
        var catalog = CreateValidCatalog();
        catalog.VoiceDialogSettings = new VoiceDialogSettings
        {
            SttMinConfidenceToAccept = acceptThreshold,
            SttMinConfidenceToConfirm = confirmThreshold
        };

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, error =>
            error.Code == PreflightCodes.ErrorSettingsInvalid
            && error.Id == "catalog"
            && error.Path == "$.voiceDialogSettings.sttMinConfidenceToConfirm");
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(85, null)]
    [InlineData(null, 65)]
    [InlineData(85, 65)]
    [InlineData(100, 100)]
    public void Validate_WhenSelectionConfidenceThresholdsAreOrderedWithinPercentRange_ReturnsOk(
        int? acceptThreshold,
        int? confirmThreshold)
    {
        var catalog = CreateValidCatalog();
        catalog.VoiceDialogSettings = new VoiceDialogSettings
        {
            SttMinConfidenceToAccept = acceptThreshold,
            SttMinConfidenceToConfirm = confirmThreshold
        };

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.True(result.Ok);
        Assert.DoesNotContain(result.Errors, error => error.Code == PreflightCodes.ErrorSettingsInvalid);
    }

    [Theory]
    [InlineData(95, null, null, 90)]
    [InlineData(null, 50, 55, null)]
    public void Validate_WhenApplicationConfidenceThresholdInheritsCatalogPartner_ReturnsOk(
        int? catalogAcceptThreshold,
        int? catalogConfirmThreshold,
        int? applicationAcceptThreshold,
        int? applicationConfirmThreshold)
    {
        var catalog = CreateValidCatalog();
        catalog.VoiceDialogSettings = new VoiceDialogSettings
        {
            SttMinConfidenceToAccept = catalogAcceptThreshold,
            SttMinConfidenceToConfirm = catalogConfirmThreshold
        };
        catalog.Applications[0].VoiceDialogSettings = new VoiceDialogSettings
        {
            SttMinConfidenceToAccept = applicationAcceptThreshold,
            SttMinConfidenceToConfirm = applicationConfirmThreshold
        };

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.True(result.Ok);
        Assert.DoesNotContain(result.Errors, error => error.Code == PreflightCodes.ErrorSettingsInvalid);
    }

    [Fact]
    public void Validate_WhenPauseBetweenItemsMsIsSchemaMaximum_ReturnsOk()
    {
        var catalog = CreateValidCatalog();
        catalog.VoiceDialogSettings = new VoiceDialogSettings
        {
            PauseBetweenItemsMs = CatalogVoiceDialogSettingBounds.PauseBetweenItemsMsMax
        };

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.True(result.Ok);
        Assert.DoesNotContain(result.Errors, error => error.Path == "$.voiceDialogSettings.pauseBetweenItemsMs");
    }

    [Fact]
    public void Validate_WhenPauseBetweenItemsMsExceedsSchemaMaximum_ReturnsError()
    {
        var catalog = CreateValidCatalog();
        catalog.VoiceDialogSettings = new VoiceDialogSettings
        {
            PauseBetweenItemsMs = CatalogVoiceDialogSettingBounds.PauseBetweenItemsMsMax + 1
        };

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, error =>
            error.Code == PreflightCodes.ErrorSettingsInvalid
            && error.Path == "$.voiceDialogSettings.pauseBetweenItemsMs");
    }

    [Fact]
    public void Validate_WhenCancelRefractoryMsExceedsSchemaMaximum_ReturnsError()
    {
        var catalog = CreateValidCatalog();
        catalog.VoiceDialogSettings = new VoiceDialogSettings
        {
            CancelRefractoryMs = CatalogVoiceDialogSettingBounds.CancelRefractoryMsMax + 1
        };

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, error =>
            error.Code == PreflightCodes.ErrorSettingsInvalid
            && error.Path == "$.voiceDialogSettings.cancelRefractoryMs");
    }

    [Fact]
    public void Validate_WhenTagsContainNonAscii_ReturnsWarning()
    {
        var catalog = CreateValidCatalog();
        catalog.Applications[0].Tags = ["büro", "formulär"];

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.True(result.Ok);
        Assert.Equal(2, result.Warnings.Count);
        Assert.All(result.Warnings, w => Assert.Equal(PreflightCodes.WarnTagNonAscii, w.Code));

        Assert.All(result.Warnings, w => Assert.Equal("app1", w.Id));
        Assert.Contains(result.Warnings, w => w.Path == "$.applications[0].tags[0]");
        Assert.Contains(result.Warnings, w => w.Path == "$.applications[0].tags[1]");
    }

    [Fact]
    public void Validate_WhenSelectionDialogIntentLocalizationMissing_ReturnsError()
    {
        var catalog = CreateValidCatalog();
        catalog.Applications[0].SupportedLanguages = ["de", "en"];
        catalog.Applications[0].DefaultLanguage = "de";
        catalog.Applications[0].DisplayName["en"] = "App 1";
        catalog.Applications[0].Description["en"] = "Description 1";
        catalog.Applications[0].Forms[0].DisplayName["en"] = "Form 1";
        catalog.Applications[0].Forms[0].Description["en"] = "Form Description";
        catalog.SelectionDialog = new ApplicationSelectionDialog
        {
            SelectionDialogId = "default.selectionDialog",
            ModelVersion = "1",
            SupportedLanguages = ["de", "en"],
            StrictLocalization = true,
            DefaultLanguage = "de",
            Intents =
            {
                new CatalogVoiceIntent
                {
                    IntentId = CatalogVoiceIntentId.Help,
                    MatchMode = CatalogVoiceIntentMatchMode.Phrase,
                    Utterances =
                    [
                        new LocalizedText
                        {
                            ["de"] = "hilfe"
                        }
                    ]
                }
            }
        };

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, error =>
            error.Code == PreflightCodes.ErrorLocalizationMissing
            && error.Path == "$.selectionDialog.intents[0].utterances[0]");
    }

    [Fact]
    public void Validate_WhenSelectionDialogNumberWordValueIsDuplicated_ReturnsError()
    {
        var catalog = CreateValidCatalog();
        catalog.SelectionDialog = new ApplicationSelectionDialog
        {
            SelectionDialogId = "default.selectionDialog",
            ModelVersion = "1",
            SupportedLanguages = ["de"],
            StrictLocalization = true,
            DefaultLanguage = "de",
            NumberWords =
            {
                new CatalogVoiceNumberWord
                {
                    Value = 1,
                    Utterances =
                    [
                        new LocalizedText
                        {
                            ["de"] = "eins"
                        },
                    ]
                },
                new CatalogVoiceNumberWord
                {
                    Value = 1,
                    Utterances =
                    [
                        new LocalizedText
                        {
                            ["de"] = "erste"
                        }
                    ]
                }
            }
        };

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, error =>
            error.Code == PreflightCodes.ErrorIdDuplicate
            && error.Path == "$.selectionDialog.numberWords[1].value");
    }

    [Fact]
    public void SelectionNamePreflightCodes_PreserveStableWireValues()
    {
        Assert.Equal("E_SELECTION_NAME_EMPTY", PreflightCodes.ErrorSelectionNameEmpty);
        Assert.Equal("E_SELECTION_NAME_DUPLICATE", PreflightCodes.ErrorSelectionNameDuplicate);
        Assert.Equal("E_SELECTION_NAME_INTENT_COLLISION", PreflightCodes.ErrorSelectionNameIntentCollision);
        Assert.Equal("E_SELECTION_NAME_WAKE_COLLISION", PreflightCodes.ErrorSelectionNameWakeCollision);
    }

    [Fact]
    public void Validate_WhenApplicationDisplayNameIsPunctuationOnly_ReturnsResolvedKeyErrorsForBothRegisters()
    {
        var catalog = CreateValidCatalog();
        catalog.Applications[0].DisplayName["de"] = "...!?";

        var result = FormsCatalogPreflight.Validate(catalog);

        var errors = result.Errors
            .Where(error => error.Code == PreflightCodes.ErrorSelectionNameEmpty)
            .ToArray();
        Assert.Equal(2, errors.Length);
        Assert.All(errors, error =>
        {
            Assert.Equal("$.applications[0].displayName['de']", error.Path);
            Assert.Equal("app1", error.Id);
            Assert.Contains("empty compact-normalized key ''", error.Message, StringComparison.Ordinal);
        });
        Assert.Contains(errors, error => error.Message.Contains("register 'formal'", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Message.Contains("register 'informal'", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WhenFormDisplayNameIsPunctuationOnly_ReturnsResolvedKeyErrorsForBothRegisters()
    {
        var catalog = CreateValidCatalog();
        catalog.Applications[0].Forms[0].DisplayName["de"] = "---";

        var result = FormsCatalogPreflight.Validate(catalog);

        var errors = result.Errors
            .Where(error => error.Code == PreflightCodes.ErrorSelectionNameEmpty)
            .ToArray();
        Assert.Equal(2, errors.Length);
        Assert.All(errors, error =>
        {
            Assert.Equal("$.applications[0].forms[0].displayName['de']", error.Path);
            Assert.Equal("form1", error.Id);
            Assert.Contains("empty compact-normalized key ''", error.Message, StringComparison.Ordinal);
        });
        Assert.Contains(errors, error => error.Message.Contains("register 'formal'", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Message.Contains("register 'informal'", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WhenApplicationNamesShareCompactKey_ReturnsDuplicateOnLaterCatalogElement()
    {
        var catalog = CreateValidCatalog();
        catalog.Applications.Add(CreateApplication("app2", "aPP-1", "Second Form"));

        var result = FormsCatalogPreflight.Validate(catalog);

        var errors = result.Errors
            .Where(error => error.Code == PreflightCodes.ErrorSelectionNameDuplicate)
            .ToArray();
        Assert.Equal(2, errors.Length);
        Assert.All(errors, error =>
        {
            Assert.Equal("$.applications[1].displayName['de']", error.Path);
            Assert.Equal("app2", error.Id);
            Assert.Contains("normalized key 'app1'", error.Message, StringComparison.Ordinal);
            Assert.Contains("$.applications[0].displayName['de']", error.Message, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Validate_WhenFormNamesShareCompactKey_ReturnsDuplicateOnLaterForm()
    {
        var catalog = CreateValidCatalog();
        catalog.Applications[0].Forms.Add(CreateForm("form2", "fORM-1"));

        var result = FormsCatalogPreflight.Validate(catalog);

        var errors = result.Errors
            .Where(error => error.Code == PreflightCodes.ErrorSelectionNameDuplicate)
            .ToArray();
        Assert.Equal(2, errors.Length);
        Assert.All(errors, error =>
        {
            Assert.Equal("$.applications[0].forms[1].displayName['de']", error.Path);
            Assert.Equal("form2", error.Id);
            Assert.Contains("normalized key 'form1'", error.Message, StringComparison.Ordinal);
            Assert.Contains("$.applications[0].forms[0].displayName['de']", error.Message, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Validate_WhenNamesOnlyOverlapPartiallyOrAcrossAllowedScopes_DoesNotReturnDuplicate()
    {
        var catalog = CreateValidCatalog();
        catalog.Applications[0].DisplayName["de"] = "Demo";
        catalog.Applications[0].DisplayName["en"] = "Demo Plus";
        catalog.Applications[0].Description["en"] = "First application";
        catalog.Applications[0].SupportedLanguages = ["de", "en"];
        catalog.Applications[0].Forms[0].DisplayName["de"] = "Shared Form";
        catalog.Applications[0].Forms[0].DisplayName["en"] = "First English Form";
        catalog.Applications[0].Forms[0].Description["en"] = "First English description";
        catalog.Applications[0].Forms.Add(CreateForm("form2", "Demo Plus"));
        catalog.Applications[0].Forms[1].DisplayName["en"] = "Second English Form";
        catalog.Applications[0].Forms[1].Description["en"] = "Second English description";
        var secondApplication = CreateApplication("app2", "Demo Plus", "Shared Form");
        secondApplication.DisplayName["en"] = "Other English Application";
        secondApplication.Description["en"] = "Second application";
        secondApplication.SupportedLanguages = ["de", "en"];
        secondApplication.Forms[0].DisplayName["en"] = "Other English Form";
        secondApplication.Forms[0].Description["en"] = "Other English description";
        catalog.Applications.Add(secondApplication);
        catalog.SelectionDialog.SupportedLanguages = ["de", "en"];
        catalog.SelectionDialog.Prompts[0].Text["en"] = "help";
        catalog.SelectionDialog.Prompts[1].Text["en"] = "Please say yes or no.";
        catalog.SelectionDialog.Prompts[2].Text["en"] = "Please say yes or no.";
        catalog.SelectionDialog.Intents[0].Utterances[0]["en"] = "help";
        catalog.SelectionDialog.NumberWords[0].Utterances[0]["en"] = "one";

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.True(result.Ok, PreflightIssueFormatter.Format(result.Errors));
        Assert.DoesNotContain(result.Errors, error => error.Code == PreflightCodes.ErrorSelectionNameDuplicate);
    }

    [Theory]
    [InlineData("application")]
    [InlineData("form")]
    public void Validate_WhenSelectionNameIsAdjacentToButNotAnIntent_ReturnsNoSelectionError(string entityKind)
    {
        var catalog = CreateIntentCollisionCatalog(
            entityKind,
            "de",
            PolitenessLevel.Formal,
            CatalogVoiceIntentId.Help,
            CatalogVoiceIntentMatchMode.Phrase);
        var displayName = string.Equals(entityKind, "application", StringComparison.Ordinal)
            ? catalog.Applications[0].DisplayName
            : catalog.Applications[0].Forms[0].DisplayName;
        displayName["de@formal"] = "xintent help de formal commandx";

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.DoesNotContain(result.Errors, error => IsSelectionNameError(error.Code));
    }

    [Theory]
    [InlineData(CatalogVoiceIntentId.ChangeLanguage, "sprache wechseln", "sprache wechselnx deutsch")]
    [InlineData(CatalogVoiceIntentId.Search, "suche", "suchex beispiel")]
    [InlineData(CatalogVoiceIntentId.Describe, "beschreibe", "beschreibex beispiel")]
    [InlineData(CatalogVoiceIntentId.OpenCandidate, "oeffne", "oeffnex beispiel")]
    [InlineData(CatalogVoiceIntentId.SelectByNumber, "nummer", "nummerx unbekannt")]
    public void Validate_WhenPrefixIsNotFollowedByPhraseBoundary_DoesNotReturnSelectionError(
        string intentId,
        string prefix,
        string displayName)
    {
        var catalog = CreateValidCatalog();
        catalog.Applications[0].DisplayName["de"] = displayName;
        catalog.SelectionDialog.Intents.Add(new CatalogVoiceIntent
        {
            IntentId = intentId,
            MatchMode = CatalogVoiceIntentMatchMode.Prefix,
            Utterances = [new LocalizedText { ["de"] = prefix }]
        });

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.DoesNotContain(result.Errors, error => IsSelectionNameError(error.Code));
    }

    [Fact]
    public void Validate_WhenValidCatalog_ReturnsOk()
    {
        var catalog = CreateValidCatalog();

        var result = FormsCatalogPreflight.Validate(catalog);

        Assert.True(result.Ok);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    internal static FormsCatalog CreateIntentCollisionCatalog(
        string entityKind,
        string language,
        PolitenessLevel targetPoliteness,
        string intentId,
        string matchMode)
    {
        var formalUtterance = $"intent {intentId} {language} formal command";
        var informalUtterance = $"intent {intentId} {language} informal command";
        var formalName = BuildIntentCollisionName(formalUtterance, intentId, matchMode);
        var informalName = BuildIntentCollisionName(informalUtterance, intentId, matchMode);
        var targetName = targetPoliteness == PolitenessLevel.Formal ? formalName : informalName;
        var safeName = $"safe {entityKind} {language} {ToWireValue(targetPoliteness)}";

        var applicationDisplayName = CreateRegisterText(
            language,
            string.Equals(entityKind, "application", StringComparison.Ordinal) && targetPoliteness == PolitenessLevel.Formal ? targetName : safeName,
            string.Equals(entityKind, "application", StringComparison.Ordinal) && targetPoliteness == PolitenessLevel.Informal ? targetName : safeName);
        var formDisplayName = CreateRegisterText(
            language,
            string.Equals(entityKind, "form", StringComparison.Ordinal) && targetPoliteness == PolitenessLevel.Formal ? targetName : $"safe form formal {language}",
            string.Equals(entityKind, "form", StringComparison.Ordinal) && targetPoliteness == PolitenessLevel.Informal ? targetName : $"safe form informal {language}");

        var selectionDialog = new ApplicationSelectionDialog
        {
            SchemaVersion = "1",
            ModelVersion = "2",
            SelectionDialogId = "selection.intent-collision",
            SupportedLanguages = [language],
            DefaultLanguage = language,
            StrictLocalization = true
        };
        if (!string.Equals(intentId, CatalogVoiceIntentId.SelectByNumber, StringComparison.Ordinal))
        {
            selectionDialog.Intents.Add(new CatalogVoiceIntent
            {
                IntentId = intentId,
                MatchMode = matchMode,
                Utterances =
                [
                    CreateRegisterText(language, formalUtterance, informalUtterance)
                ]
            });
        }

        return new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            SelectionDialog = selectionDialog,
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "collision-app",
                    DisplayName = applicationDisplayName,
                    Description = CreateRegisterText(language, "application description formal", "application description informal"),
                    SupportedLanguages = [language],
                    DefaultLanguage = language,
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("collision-form"),
                            DisplayName = formDisplayName,
                            Description = CreateRegisterText(language, "form description formal", "form description informal"),
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        }
                    ]
                }
            ]
        };
    }

    internal static string BuildIntentCollisionName(string utterance, string intentId, string matchMode)
    {
        if (string.Equals(intentId, CatalogVoiceIntentId.SelectByNumber, StringComparison.Ordinal))
        {
            return "1";
        }

        return string.Equals(matchMode, CatalogVoiceIntentMatchMode.Prefix, StringComparison.Ordinal)
            ? $"{utterance} target"
            : utterance;
    }

    internal static LocalizedText CreateRegisterText(string language, string formal, string informal) => new()
    {
        [$"{language}@formal"] = formal,
        [$"{language}@informal"] = informal
    };

    private static ApplicationDescriptor CreateApplication(string applicationId, string displayName, string formDisplayName) => new()
    {
        ApplicationId = applicationId,
        DisplayName = new LocalizedText { ["de"] = displayName },
        Description = new LocalizedText { ["de"] = $"Description for {applicationId}" },
        SupportedLanguages = ["de"],
        DefaultLanguage = "de",
        Forms = [CreateForm($"{applicationId}-form", formDisplayName)]
    };

    private static FormDescriptor CreateForm(string formId, string displayName) => new()
    {
        FormId = FormId.From(formId),
        DisplayName = new LocalizedText { ["de"] = displayName },
        Description = new LocalizedText { ["de"] = $"Description for {formId}" },
        LatestContractVersion = "1",
        AvailableContractVersions = ["1"]
    };

    internal static bool IsSelectionNameError(string code)
    {
        return code is PreflightCodes.ErrorSelectionNameEmpty
            or PreflightCodes.ErrorSelectionNameDuplicate
            or PreflightCodes.ErrorSelectionNameIntentCollision
            or PreflightCodes.ErrorSelectionNameWakeCollision;
    }

    internal static string ToWireValue(PolitenessLevel politenessLevel)
    {
        return politenessLevel switch
        {
            PolitenessLevel.Formal => "formal",
            PolitenessLevel.Informal => "informal",
            _ => throw new ArgumentOutOfRangeException(nameof(politenessLevel), politenessLevel, "Unknown politeness level.")
        };
    }

    private static FormsCatalog CreateValidCatalog() => new()
    {
        SchemaVersion = "1",
        CatalogVersion = "1",
        SelectionDialog = new ApplicationSelectionDialog
        {
            SchemaVersion = "1",
            ModelVersion = "1",
            SelectionDialogId = "default.selectionDialog",
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            Prompts =
            [
                new CatalogVoicePrompt
                {
                    PromptId = "app.help",
                    Text = new LocalizedText { ["de"] = "hilfe" }
                },
                new CatalogVoicePrompt
                {
                    PromptId = "catalog.app.confirmation.repeat",
                    Text = new LocalizedText
                    {
                        ["de@formal"] = "Bitte sagen Sie ja oder nein.",
                        ["de@informal"] = "Bitte sag ja oder nein."
                    }
                },
                new CatalogVoicePrompt
                {
                    PromptId = "catalog.form.confirmation.repeat",
                    Text = new LocalizedText
                    {
                        ["de@formal"] = "Bitte sagen Sie ja oder nein.",
                        ["de@informal"] = "Bitte sag ja oder nein."
                    }
                },
                new CatalogVoicePrompt
                {
                    PromptId = "catalog.app.reListen",
                    Text = new LocalizedText
                    {
                        ["de@formal"] = "Verstanden. Ich höre weiter zu.",
                        ["de@informal"] = "Verstanden. Ich höre dir weiter zu.",
                        ["en"] = "Got it. I am still listening."
                    }
                },
                new CatalogVoicePrompt
                {
                    PromptId = "catalog.form.reListen",
                    Text = new LocalizedText
                    {
                        ["de@formal"] = "Verstanden. Ich höre weiter zu.",
                        ["de@informal"] = "Verstanden. Ich höre dir weiter zu.",
                        ["en"] = "Got it. I am still listening."
                    }
                }
            ],
            Intents =
            [
                new CatalogVoiceIntent
                {
                    IntentId = CatalogVoiceIntentId.Help,
                    MatchMode = CatalogVoiceIntentMatchMode.Phrase,
                    Utterances = [new LocalizedText { ["de"] = "hilfe" }]
                }
            ],
            NumberWords =
            [
                new CatalogVoiceNumberWord
                {
                    Value = 1,
                    Utterances = [new LocalizedText { ["de"] = "eins" }]
                }
            ]
        },
        Applications =
        [
            new ApplicationDescriptor
            {
                ApplicationId = "app1",
                DisplayName = new LocalizedText { ["de"] = "App 1" },
                Description = new LocalizedText { ["de"] = "Description 1" },
                SupportedLanguages = ["de"],
                DefaultLanguage = "de",
                Tags = ["test", "sample"],
                Forms =
                [
                    new FormDescriptor
                    {
                        FormId = FormId.From("form1"),
                        DisplayName = new LocalizedText { ["de"] = "Form 1" },
                        Description = new LocalizedText { ["de"] = "Form Description" },
                        LatestContractVersion = "1",
                        AvailableContractVersions = ["1"]
                    }
                ]
            }
        ]
    };
}
