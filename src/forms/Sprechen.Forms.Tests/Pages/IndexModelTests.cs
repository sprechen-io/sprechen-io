using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sprechen.Contracts.FormSurfaces;
using Sprechen.Forms.Pages;
using Sprechen.Forms.Routing;
using Sprechen.Forms.Services;
using Sprechen.Forms.Tests.TestDoubles;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Web.FormSurfaces;
using Xunit;
using ReferenceFormDefinition = Sprechen.Mda.Contracts.V1.FormSurfaces.FormSurfaceDefinition;
using ReferenceFormField = Sprechen.Mda.Contracts.V1.FormSurfaces.FormSurfaceField;
using ReferenceFormFieldGroupInfo = Sprechen.Mda.Contracts.V1.FormSurfaces.FormSurfaceFieldGroupInfo;
using ReferenceFormSurfacePresenter = Sprechen.Web.FormSurfaces.FormSurfacePresenter;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Forms.Tests.Pages;

public sealed class IndexModelTests
{
    #region Error Handling Tests

    [Fact]
    public async Task OnGetAsync_WhenCatalogLoadFails_SetsErrorCode()
    {
        var model = new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => throw new InvalidOperationException("Boom")),
            new DelegateFormContractDataClient((_, _, _, _) => throw new InvalidOperationException("Should not be called")),
            new DelegateReferenceFormFieldBuilder((_, _, _) => CreateReferenceForm()),
            new ReferenceFormSurfacePresenter()))
        {
            Language = "de"
        };

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal("CATALOG_LOAD_FAILED", model.ErrorCode);
    }

    [Fact]
    public async Task OnGetAsync_WhenContractLoadFails_SetsErrorCode()
    {
        var catalog = CreateCatalog();

        var model = new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => Task.FromResult(catalog)),
            new DelegateFormContractDataClient((_, _, _, _) => throw new InvalidOperationException("Contract load failed")),
            new DelegateReferenceFormFieldBuilder((_, _, _) => CreateReferenceForm()),
            new ReferenceFormSurfacePresenter()))
        {
            Language = "de",
            ApplicationId = "sample.checkout"
        };

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal("FORM_CONTRACT_LOAD_FAILED", model.ErrorCode);
    }

    #endregion

    #region Application Selection Tests

    [Fact]
    public async Task OnGetAsync_WhenSingleFormAppSelected_AutoSelectsFormAndBuildsFields()
    {
        var catalog = CreateCatalog();
        var contract = CreateContract();
        var expectedFields = new[]
        {
            new ReferenceFormField(
                fieldId: "email",
                fieldGroupId: "entry",
                label: "E-Mail",
                valueTypeKind: "string",
                codeStyle: false,
                inputType: "email",
                required: true,
                isUserEditable: true,
                helpText: null,
                placeholder: null)
        };

        var model = new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => Task.FromResult(catalog)),
            new DelegateFormContractDataClient((_, _, _, _) => Task.FromResult(contract)),
            new DelegateReferenceFormFieldBuilder((_, _, _) => CreateReferenceForm(expectedFields)),
            new ReferenceFormSurfacePresenter()))
        {
            Language = "de",
            ApplicationId = "sample.checkout"
        };

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal("de", model.SelectedLanguage);
        Assert.Equal("sample.checkout", model.SelectedApplicationId);
        Assert.Equal("address-demo", model.SelectedFormId);
        Assert.Single(model.Fields);
        Assert.Equal(expectedFields[0], model.Fields[0]);
        Assert.NotNull(model.Surface);
        Assert.Single(model.Surface!.FieldGroups);

        Assert.Single(model.Applications);
        Assert.Single(model.Forms);
    }

    [Fact]
    public async Task OnGetAsync_WithNoApplicationId_OnlySetsLanguageAndApplications()
    {
        var catalog = CreateCatalog();

        var model = new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => Task.FromResult(catalog)),
            new DelegateFormContractDataClient((_, _, _, _) => throw new InvalidOperationException("Should not be called")),
            new DelegateReferenceFormFieldBuilder((_, _, _) => CreateReferenceForm()),
            new ReferenceFormSurfacePresenter()))
        {
            Language = "de"
        };

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal("de", model.SelectedLanguage);
        Assert.Equal(string.Empty, model.SelectedApplicationId);
        Assert.Single(model.Applications);
    }

    [Fact]
    public async Task OnGetAsync_WithInvalidApplicationId_ClearsSelection()
    {
        var catalog = CreateCatalog();

        var model = new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => Task.FromResult(catalog)),
            new DelegateFormContractDataClient((_, _, _, _) => throw new InvalidOperationException("Should not be called")),
            new DelegateReferenceFormFieldBuilder((_, _, _) => CreateReferenceForm()),
            new ReferenceFormSurfacePresenter()))
        {
            Language = "de",
            ApplicationId = "nonexistent.app"
        };

        await model.OnGetAsync(CancellationToken.None);


        Assert.Equal(string.Empty, model.SelectedApplicationId);
    }

    [Fact]
    public async Task OnGetAsync_WithApplicationNotSupportingLanguage_FiltersByLanguage()
    {
        var catalog = CreateCatalog();

        var model = new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => Task.FromResult(catalog)),
            new DelegateFormContractDataClient((_, _, _, _) => throw new InvalidOperationException("Should not be called")),
            new DelegateReferenceFormFieldBuilder((_, _, _) => CreateReferenceForm()),
            new ReferenceFormSurfacePresenter()))
        {
            Language = "en"
        };

        await model.OnGetAsync(CancellationToken.None);

        Assert.Single(model.Applications);
        Assert.Equal("sample.english-only", model.Applications[0].Id);
    }

    #endregion

    #region Form Selection Tests

    [Fact]
    public async Task OnGetAsync_WithMultipleForms_ShowsAllForms()
    {
        var catalog = CreateCatalogWithMultipleForms();

        var model = new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => Task.FromResult(catalog)),
            new DelegateFormContractDataClient((_, _, _, _) => throw new InvalidOperationException("Should not be called")),
            new DelegateReferenceFormFieldBuilder((_, _, _) => CreateReferenceForm()),
            new ReferenceFormSurfacePresenter()))
        {
            Language = "de",
            ApplicationId = "sample.multi"
        };

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal(2, model.Forms.Count);

        Assert.Equal(string.Empty, model.SelectedFormId);
    }

    [Fact]
    public async Task OnGetAsync_WithExplicitFormId_SelectsSpecifiedForm()
    {
        var catalog = CreateCatalogWithMultipleForms();
        var contract = new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "sample.multi",
            FormId = FormId.From("form2"),
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            EntryFieldGroupId = FieldGroupId.From("entry"),
            StrictLocalization = false
        };

        var model = new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => Task.FromResult(catalog)),
            new DelegateFormContractDataClient((_, _, _, _) => Task.FromResult(contract)),
            new DelegateReferenceFormFieldBuilder((_, _, _) => CreateReferenceForm()),
            new ReferenceFormSurfacePresenter()))
        {
            Language = "de",
            ApplicationId = "sample.multi",
            FormId = "form2"
        };

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal("form2", model.SelectedFormId);
    }

    [Fact]
    public async Task OnGetAsync_WhenApplicationChangedFromPrevious_ResetsFormSelection()
    {
        var catalog = new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "sample.multi",
                    DisplayName = new LocalizedText { ["de"] = "Mehrfach-Formular" },
                    Description = new LocalizedText { ["de"] = "Demo" },
                    Tags = ["sample"],
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("form1"),
                            DisplayName = new LocalizedText { ["de"] = "Form 1" },
                            Description = new LocalizedText { ["de"] = "Demo" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        },
                        new FormDescriptor
                        {
                            FormId = FormId.From("form2"),
                            DisplayName = new LocalizedText { ["de"] = "Form 2" },
                            Description = new LocalizedText { ["de"] = "Demo" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        }
                    ]
                },
                new ApplicationDescriptor
                {
                    ApplicationId = "sample.checkout",
                    DisplayName = new LocalizedText { ["de"] = "Checkout" },
                    Description = new LocalizedText { ["de"] = "Demo" },
                    Tags = ["sample"],
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("checkout-form-1"),
                            DisplayName = new LocalizedText { ["de"] = "Checkout Formular 1" },
                            Description = new LocalizedText { ["de"] = "Demo" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        },
                        new FormDescriptor
                        {
                            FormId = FormId.From("checkout-form-2"),
                            DisplayName = new LocalizedText { ["de"] = "Checkout Formular 2" },
                            Description = new LocalizedText { ["de"] = "Demo" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        }
                    ]
                }
            ]
        };

        var model = new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => Task.FromResult(catalog)),
            new DelegateFormContractDataClient((_, _, _, _) => throw new InvalidOperationException("Should not be called")),
            new DelegateReferenceFormFieldBuilder((_, _, _) => CreateReferenceForm()),
            new ReferenceFormSurfacePresenter()))
        {
            Language = "de",
            PreviousApplicationId = "sample.multi",
            ApplicationId = "sample.checkout",
            FormId = "form1"
        };

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal(string.Empty, model.SelectedFormId);
        Assert.Equal("sample.checkout", model.SelectedApplicationId);
        Assert.Equal(2, model.Forms.Count);
    }

    [Fact]
    public async Task OnGetAsync_WhenApplicationUnchanged_UsesFormFromQuery()
    {
        var catalog = CreateCatalogWithMultipleForms();
        var contract = new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "sample.multi",
            FormId = FormId.From("form2"),
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            EntryFieldGroupId = FieldGroupId.From("entry"),
            StrictLocalization = false
        };

        var model = new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => Task.FromResult(catalog)),
            new DelegateFormContractDataClient((_, _, _, _) => Task.FromResult(contract)),
            new DelegateReferenceFormFieldBuilder((_, _, _) => CreateReferenceForm()),
            new ReferenceFormSurfacePresenter()))
        {
            Language = "de",
            PreviousApplicationId = "sample.multi",
            ApplicationId = "sample.multi",
            FormId = "form2"
        };

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal("form2", model.SelectedFormId);
    }

    [Fact]
    public async Task OnGetAsync_WithInvalidFormId_ClearsSelection()
    {
        var catalog = CreateCatalog();

        var model = new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => Task.FromResult(catalog)),
            new DelegateFormContractDataClient((_, _, _, _) => throw new InvalidOperationException("Should not be called")),
            new DelegateReferenceFormFieldBuilder((_, _, _) => CreateReferenceForm()),
            new ReferenceFormSurfacePresenter()))
        {
            Language = "de",
            ApplicationId = "sample.checkout",
            FormId = "nonexistent-form"
        };

        await model.OnGetAsync(CancellationToken.None);


        Assert.NotEqual("nonexistent-form", model.SelectedFormId);
    }

    #endregion

    #region Study Parameter Tests

    [Fact]
    public async Task OnGetAsync_OperationalProbeUsesMarkedSurveyAndNeverBindsParticipant()
    {
        var model = new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => Task.FromResult(CreateStudyCatalog())),
            new DelegateFormContractDataClient((_, _, _, _) => Task.FromResult(CreateContract())),
            new DelegateReferenceFormFieldBuilder((_, _, _) => CreateReferenceForm()),
            new ReferenceFormSurfacePresenter()))
        {
            Language = "de",
            ApplicationId = "sample.other",
            FormId = "arbitrary-form"
        };
        BindStudyLink(model, StudyLinkIdentity.OperationalProbe());

        await model.OnGetAsync(CancellationToken.None);

        Assert.True(model.IsOperationalProbe);
        Assert.False(model.IsStudyLinkRejected);
        Assert.Equal(string.Empty, model.StudyParticipantId);
        Assert.Equal("sample.checkout", model.SelectedApplicationId);
        Assert.Equal("address-demo", model.SelectedFormId);
    }

    [Fact]
    public async Task OnGetAsync_WithStudyParameters_SetsStudyContext()
    {
        var catalog = CreateStudyCatalog();
        var contract = CreateContract();

        var model = new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => Task.FromResult(catalog)),
            new DelegateFormContractDataClient((_, _, _, _) => Task.FromResult(contract)),
            new DelegateReferenceFormFieldBuilder((_, _, _) => CreateReferenceForm()),
            new ReferenceFormSurfacePresenter()))
        {
            Language = "de"
        };
        BindStudyLink(model, StudyLinkIdentity.Accepted("x9jdln"));

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal("x9jdln", model.StudyParticipantId);
        Assert.Equal(string.Empty, model.StudyTaskId);
        Assert.Equal("A", model.StudyCondition);
        Assert.False(model.IsStudyLinkRejected);
    }

    [Fact]
    public async Task OnGetAsync_WithStudyToken_LoadsStudyFormInsteadOfSelection()
    {
        var catalog = CreateStudyCatalog();
        var contract = CreateContract();
        var expectedFields = new[]
        {
            new ReferenceFormField(
                fieldId: "email",
                fieldGroupId: "entry",
                label: "E-Mail",
                valueTypeKind: "string",
                codeStyle: false,
                inputType: "email",
                required: true,
                isUserEditable: true,
                helpText: null,
                placeholder: null)
        };

        var model = new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => Task.FromResult(catalog)),
            new DelegateFormContractDataClient((_, _, _, _) => Task.FromResult(contract)),
            new DelegateReferenceFormFieldBuilder((_, _, _) => CreateReferenceForm(expectedFields)),
            new ReferenceFormSurfacePresenter()))
        {
            Language = "de",
            ApplicationId = "sample.other"
        };
        BindStudyLink(model, StudyLinkIdentity.Accepted("x9jdln"));

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal("de", model.SelectedLanguage);
        Assert.Equal("sample.checkout", model.SelectedApplicationId);
        Assert.Equal("address-demo", model.SelectedFormId);
        Assert.NotNull(model.Surface);
        Assert.NotEmpty(model.Surface!.FieldGroups);
        Assert.Equal("x9jdln", model.StudyParticipantId);
        Assert.False(model.IsStudyLinkRejected);
        Assert.Null(model.ErrorCode);
    }

    [Fact]
    public async Task OnGetAsync_WithStudyToken_KeepsRequestedLanguage()
    {
        var catalog = CreateStudyCatalog();
        var contract = CreateContract();
        contract.SupportedLanguages = ["de", "en"];

        var model = new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => Task.FromResult(catalog)),
            new DelegateFormContractDataClient((_, _, _, _) => Task.FromResult(contract)),
            new DelegateReferenceFormFieldBuilder((_, _, _) => CreateReferenceForm(CreateEmailField())),
            new ReferenceFormSurfacePresenter()))
        {
            Language = "en"
        };
        BindStudyLink(model, StudyLinkIdentity.Accepted("x9jdln"));

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal("en", model.SelectedLanguage);
        Assert.Equal("sample.checkout", model.SelectedApplicationId);
        Assert.NotNull(model.Surface);
    }

    [Fact]
    public async Task OnGetAsync_WithoutStudyToken_DoesNotBindStudyForm()
    {
        var catalog = CreateStudyCatalog();

        var model = new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => Task.FromResult(catalog)),
            new DelegateFormContractDataClient((_, _, _, _) => throw new InvalidOperationException("Should not be called")),
            new DelegateReferenceFormFieldBuilder((_, _, _) => CreateReferenceForm()),
            new ReferenceFormSurfacePresenter()))
        {
            Language = "de"
        };

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal("de", model.SelectedLanguage);
        Assert.Equal(string.Empty, model.SelectedApplicationId);
        Assert.Null(model.Surface);
        Assert.Equal(string.Empty, model.StudyParticipantId);
        Assert.Contains(model.Applications, application => application.Id == "sample.checkout");
        Assert.Contains(model.Applications, application => application.Id == "sample.other");
    }

    [Fact]
    public async Task OnGetAsync_WithoutStudyParameters_DefaultsToEmptyOrA()
    {
        var catalog = CreateCatalog();
        var contract = CreateContract();

        var model = new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => Task.FromResult(catalog)),
            new DelegateFormContractDataClient((_, _, _, _) => Task.FromResult(contract)),
            new DelegateReferenceFormFieldBuilder((_, _, _) => CreateReferenceForm()),
            new ReferenceFormSurfacePresenter()))
        {
            Language = "de",
            ApplicationId = "sample.checkout"
        };

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal(string.Empty, model.StudyParticipantId);
        Assert.Equal(string.Empty, model.StudyTaskId);
        Assert.Equal("A", model.StudyCondition);
    }

    [Fact]
    public async Task OnGetAsync_WithRejectedStudyLink_DoesNotLoadSelection()
    {
        var catalog = CreateCatalog();
        var contract = CreateContract();

        var model = new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => Task.FromResult(catalog)),
            new DelegateFormContractDataClient((_, _, _, _) => Task.FromResult(contract)),
            new DelegateReferenceFormFieldBuilder((_, _, _) => CreateReferenceForm()),
            new ReferenceFormSurfacePresenter()))
        {
            Language = "de"
        };
        BindStudyLink(model, StudyLinkIdentity.Rejected("segments"));

        await model.OnGetAsync(CancellationToken.None);

        Assert.True(model.IsStudyLinkRejected);
        Assert.Equal("segments", model.StudyLinkRejectionReason);
        Assert.Empty(model.Applications);
        Assert.Null(model.Surface);
    }

    #endregion

    #region Navigation Tests

    [Fact]
    public async Task OnGetAsync_WithMultipleFormsInCatalog_SetsProgressInfo()
    {
        var catalog = CreateCatalogWithMultipleForms();
        var contract = new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "sample.multi",
            FormId = FormId.From("form1"),
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            EntryFieldGroupId = FieldGroupId.From("entry"),
            StrictLocalization = false
        };

        var model = new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => Task.FromResult(catalog)),
            new DelegateFormContractDataClient((_, _, _, _) => Task.FromResult(contract)),
            new DelegateReferenceFormFieldBuilder((_, _, _) => CreateReferenceForm()),
            new ReferenceFormSurfacePresenter()))
        {
            Language = "de",
            ApplicationId = "sample.multi",
            FormId = "form1"
        };

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal(2, model.TotalFormsCount);
        Assert.Equal(1, model.CurrentFormIndex);
    }

    [Fact]
    public async Task OnGetAsync_WithSingleForm_SetsProgressInfoCorrectly()
    {
        var catalog = CreateCatalog();
        var contract = CreateContract();

        var model = new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => Task.FromResult(catalog)),
            new DelegateFormContractDataClient((_, _, _, _) => Task.FromResult(contract)),
            new DelegateReferenceFormFieldBuilder((_, _, _) => CreateReferenceForm()),
            new ReferenceFormSurfacePresenter()))
        {
            Language = "de",
            ApplicationId = "sample.checkout"
        };

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal(1, model.TotalFormsCount);
        Assert.Equal(1, model.CurrentFormIndex);
    }

    #endregion

    #region Field Building Tests

    [Fact]
    public async Task OnGetAsync_WhenFieldBuildingFails_CausesContractLoadError()
    {
        var catalog = CreateCatalog();
        var contract = CreateContract();

        var model = new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => Task.FromResult(catalog)),
            new DelegateFormContractDataClient((_, _, _, _) => Task.FromResult(contract)),
            new DelegateReferenceFormFieldBuilder((_, _, _) => throw new InvalidOperationException("Field build failed")),
            new ReferenceFormSurfacePresenter()))
        {
            Language = "de",
            ApplicationId = "sample.checkout"
        };

        await model.OnGetAsync(CancellationToken.None);

        // Feldaufbau und Vertragsladen verwenden dieselbe Fehlerbehandlung.
        Assert.Equal("FORM_CONTRACT_LOAD_FAILED", model.ErrorCode);
    }

    [Fact]
    public async Task OnGetAsync_WithValidContract_PassesLanguageToFieldBuilder()
    {
        var catalog = CreateCatalog();
        var contract = CreateContract();
        string? capturedLanguage = null;

        var model = new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => Task.FromResult(catalog)),
            new DelegateFormContractDataClient((_, _, _, _) => Task.FromResult(contract)),
            new DelegateReferenceFormFieldBuilder((_, lang, _) =>
            {
                capturedLanguage = lang;
                return CreateReferenceForm();
            }),
            new ReferenceFormSurfacePresenter()))
        {
            Language = "de",
            ApplicationId = "sample.checkout"
        };

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal("de", capturedLanguage);
    }

    [Fact]
    public async Task OnGetAsync_WhenApplicationUsesInformalPoliteness_PassesPolitenessToBuilderAndContractTextResolution()
    {
        var catalog = CreateCatalog();
        catalog.Applications[0].VoiceDialogSettings = new VoiceDialogSettings
        {
            PolitenessLevel = PolitenessLevel.Informal
        };

        var contract = CreateContract();
        contract.DisplayName = new LocalizedText
        {
            ["de@formal"] = "Ihre Kontaktdaten",
            ["de@informal"] = "Deine Kontaktdaten"
        };
        contract.Description = new LocalizedText
        {
            ["de@formal"] = "Bitte pruefen Sie Ihre Angaben.",
            ["de@informal"] = "Pruefe deine Angaben."
        };

        PolitenessLevel? capturedPoliteness = null;

        var model = new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => Task.FromResult(catalog)),
            new DelegateFormContractDataClient((_, _, _, _) => Task.FromResult(contract)),
            new DelegateReferenceFormFieldBuilder((_, _, politenessLevel) =>
            {
                capturedPoliteness = politenessLevel;
                return CreateReferenceForm();
            }),
            new ReferenceFormSurfacePresenter()))
        {
            Language = "de",
            ApplicationId = "sample.checkout"
        };

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal(PolitenessLevel.Informal, capturedPoliteness);
        Assert.Equal("Deine Kontaktdaten", model.FormDisplayName);
        Assert.Equal("Pruefe deine Angaben.", model.FormDescription);
    }

    #endregion

    #region Default Language Fallback Tests

    [Fact]
    public async Task OnGetAsync_WithNullLanguage_UsesBrowserLanguageWhenSupported()
    {
        var catalog = CreateCatalog();

        var model = new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => Task.FromResult(catalog)),
            new DelegateFormContractDataClient((_, _, _, _) => throw new InvalidOperationException("Should not be called")),
            new DelegateReferenceFormFieldBuilder((_, _, _) => CreateReferenceForm()),
            new ReferenceFormSurfacePresenter()))
        {
            Language = null
        };

        SetAcceptLanguage(model, "en-US,en;q=0.9");

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal("en", model.SelectedLanguage);
    }

    [Fact]
    public async Task OnGetAsync_WithNullLanguageAndUnsupportedBrowserLanguage_DefaultsToDe()
    {
        var catalog = CreateCatalog();

        var model = new IndexModel(new IndexModelTestFactory().CreateRuntime(
            new DelegateFormsCatalogDataClient(_ => Task.FromResult(catalog)),
            new DelegateFormContractDataClient((_, _, _, _) => throw new InvalidOperationException("Should not be called")),
            new DelegateReferenceFormFieldBuilder((_, _, _) => CreateReferenceForm()),
            new ReferenceFormSurfacePresenter()))
        {
            Language = null
        };

        SetAcceptLanguage(model, "pl-PL,pl;q=0.9");

        await model.OnGetAsync(CancellationToken.None);

        Assert.Equal("de", model.SelectedLanguage);
    }

    #endregion

    #region Helper Methods

    private static ReferenceFormField[] CreateEmailField()
    {
        return
        [
            new ReferenceFormField(
                fieldId: "email",
                fieldGroupId: "entry",
                label: "E-Mail",
                valueTypeKind: "string",
                codeStyle: false,
                inputType: "email",
                required: true,
                isUserEditable: true,
                helpText: null,
                placeholder: null)
        ];
    }

    private static FormsCatalog CreateStudyCatalog()
    {
        return new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "sample.checkout",
                    DisplayName = new LocalizedText { ["de"] = "Checkout", ["en"] = "Checkout" },
                    Description = new LocalizedText { ["de"] = "Demo", ["en"] = "Demo" },
                    Tags = ["sample"],
                    SupportedLanguages = ["de", "en"],
                    DefaultLanguage = "de",
                    IsStudyApplication = true,
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("address-demo"),
                            DisplayName = new LocalizedText { ["de"] = "Adresse", ["en"] = "Address" },
                            Description = new LocalizedText { ["de"] = "Demo", ["en"] = "Demo" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        }
                    ]
                },
                new ApplicationDescriptor
                {
                    ApplicationId = "sample.other",
                    DisplayName = new LocalizedText { ["de"] = "Andere" },
                    Description = new LocalizedText { ["de"] = "Demo" },
                    Tags = ["sample"],
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("other-form"),
                            DisplayName = new LocalizedText { ["de"] = "Andere" },
                            Description = new LocalizedText { ["de"] = "Demo" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        }
                    ]
                }
            ]
        };
    }

    private static FormsCatalog CreateCatalog()
    {
        return new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "sample.checkout",
                    DisplayName = new LocalizedText { ["de"] = "Checkout" },
                    Description = new LocalizedText { ["de"] = "Demo" },
                    Tags = ["sample"],
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("address-demo"),
                            DisplayName = new LocalizedText { ["de"] = "Adresse" },
                            Description = new LocalizedText { ["de"] = "Demo" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        }
                    ]
                },
                new ApplicationDescriptor
                {
                    ApplicationId = "sample.english-only",
                    DisplayName = new LocalizedText { ["en"] = "English app" },
                    Description = new LocalizedText { ["en"] = "Demo" },
                    Tags = ["sample"],
                    SupportedLanguages = ["en"],
                    DefaultLanguage = "en",
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("demo"),
                            DisplayName = new LocalizedText { ["en"] = "Demo" },
                            Description = new LocalizedText { ["en"] = "Demo" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        }
                    ]
                }
            ]
        };
    }

    private static FormsCatalog CreateCatalogWithMultipleForms()
    {
        return new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "sample.multi",
                    DisplayName = new LocalizedText { ["de"] = "Multi-Form" },
                    Description = new LocalizedText { ["de"] = "Demo" },
                    Tags = ["sample"],
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("form1"),
                            DisplayName = new LocalizedText { ["de"] = "Form 1" },
                            Description = new LocalizedText { ["de"] = "Demo" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        },
                        new FormDescriptor
                        {
                            FormId = FormId.From("form2"),
                            DisplayName = new LocalizedText { ["de"] = "Form 2" },
                            Description = new LocalizedText { ["de"] = "Demo" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        }
                    ]
                }
            ]
        };
    }

    private static ReferenceFormDefinition CreateReferenceForm(params ReferenceFormField[] fields)
    {
        return new ReferenceFormDefinition(
            entryFieldGroupId: "entry",
            fieldGroupsById: new Dictionary<string, ReferenceFormFieldGroupInfo>(StringComparer.OrdinalIgnoreCase),
            fields: fields);
    }

    private static FormContract CreateContract()
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "sample.checkout",
            FormId = FormId.From("address-demo"),
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            EntryFieldGroupId = FieldGroupId.From("entry"),
            StrictLocalization = false
        };
    }

    private static void SetAcceptLanguage(IndexModel model, string acceptLanguage)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["Accept-Language"] = acceptLanguage;
        model.PageContext = new PageContext { HttpContext = context };
    }

    private static void BindStudyLink(IndexModel model, StudyLinkIdentity identity)
    {
        if (model.PageContext?.HttpContext is null)
        {
            model.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };
        }

        model.HttpContext.Features.Set(identity);
    }

    #endregion
}








