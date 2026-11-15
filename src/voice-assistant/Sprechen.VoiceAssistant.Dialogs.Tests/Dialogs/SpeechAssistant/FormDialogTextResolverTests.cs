using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Xunit;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormDialogTextResolverTests
{
    #region TryResolveFieldLabel Tests

    [Fact]
    public void TryResolveFieldLabel_MatchingVariableId_ReturnsLabel()
    {
        var contract = CreateContractWithField("firstName", new LocalizedText { ["de"] = "Vorname", ["en"] = "First Name" });

        var result = FormDialogTextResolver.TryResolveFieldLabel(contract, "firstName", "de");

        Assert.Equal("Vorname", result);
    }

    [Fact]
    public void TryResolveFieldLabel_EnglishFallback_ReturnsEnglishLabel()
    {
        var contract = CreateContractWithField("firstName", new LocalizedText { ["en"] = "First Name" });

        var result = FormDialogTextResolver.TryResolveFieldLabel(contract, "firstName", "de");

        Assert.Equal("First Name", result);
    }

    [Fact]
    public void TryResolveFieldLabel_NoMatchingVariable_ReturnsNull()
    {
        var contract = CreateContractWithField("firstName", new LocalizedText { ["de"] = "Vorname" });

        var result = FormDialogTextResolver.TryResolveFieldLabel(contract, "lastName", "de");

        Assert.Null(result);
    }

    [Fact]
    public void TryResolveFieldLabel_NullContract_ReturnsNull()
    {
        var result = FormDialogTextResolver.TryResolveFieldLabel(null!, "firstName", "de");

        Assert.Null(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TryResolveFieldLabel_InvalidVariableId_ReturnsNull(string? variableId)
    {
        var contract = CreateContractWithField("firstName", new LocalizedText { ["de"] = "Vorname" });

        var result = FormDialogTextResolver.TryResolveFieldLabel(contract, variableId!, "de");

        Assert.Null(result);
    }

    #endregion

    #region ResolveSourceFormName Tests

    [Fact]
    public void ResolveSourceFormName_MatchingFormId_ReturnsLocalizedName()
    {
        var app = CreateApplicationWithForm("main-form", new LocalizedText { ["de"] = "Hauptformular", ["en"] = "Main Form" });

        var result = FormDialogTextResolver.ResolveSourceFormName(app, "main-form", "de");

        Assert.Equal("Hauptformular", result);
    }

    [Fact]
    public void ResolveSourceFormName_NoMatchingForm_ReturnsFormId()
    {
        var app = CreateApplicationWithForm("main-form", new LocalizedText { ["de"] = "Hauptformular" });

        var result = FormDialogTextResolver.ResolveSourceFormName(app, "other-form", "de");

        Assert.Equal("other-form", result);
    }

    [Fact]
    public void ResolveSourceFormName_NullApplication_ReturnsFormId()
    {
        var result = FormDialogTextResolver.ResolveSourceFormName(null!, "main-form", "de");

        Assert.Equal("main-form", result);
    }

    #endregion

    #region ResolveCatalogText Tests

    [Fact]
    public void ResolveCatalogText_ExactLanguageMatch_ReturnsLocalizedText()
    {
        var app = CreateApplicationWithForm("main", new LocalizedText { ["de"] = "Test" });
        var text = new LocalizedText { ["de"] = "Hallo", ["en"] = "Hello" };

        var result = FormDialogTextResolver.ResolveCatalogText(app, text, "de", "default");

        Assert.Equal("Hallo", result);
    }

    [Fact]
    public void ResolveCatalogText_FallbackToDefault_ReturnsDefaultLanguage()
    {
        var app = CreateApplication("test-app", defaultLanguage: "en");
        var text = new LocalizedText { ["en"] = "Hello" };

        var result = FormDialogTextResolver.ResolveCatalogText(app, text, "de", "default");

        Assert.Equal("Hello", result);
    }

    [Fact]
    public void ResolveCatalogText_NullText_ReturnsFallback()
    {
        var app = CreateApplication("test-app");

        var result = FormDialogTextResolver.ResolveCatalogText(app, null, "de", "fallback");

        Assert.Equal("fallback", result);
    }

    #endregion

    #region ResolveForm Tests

    [Fact]
    public void ResolveForm_MatchingFormId_ReturnsForm()
    {
        var app = CreateApplicationWithForms(
            ("form1", "Form 1"),
            ("form2", "Form 2"));

        var result = FormDialogTextResolver.ResolveForm(app, "form2");

        Assert.NotNull(result);
        Assert.Equal("form2", result.FormId);
    }

    [Fact]
    public void ResolveForm_NullFormId_ReturnsFirstForm()
    {
        var app = CreateApplicationWithForms(
            ("form1", "Form 1"),
            ("form2", "Form 2"));

        var result = FormDialogTextResolver.ResolveForm(app, null);

        Assert.NotNull(result);
        Assert.Equal("form1", result.FormId);
    }

    [Fact]
    public void ResolveForm_NoMatchingForm_ReturnsNull()
    {
        var app = CreateApplicationWithForms(("form1", "Form 1"));

        var result = FormDialogTextResolver.ResolveForm(app, "nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public void ResolveForm_NullApplication_ReturnsNull()
    {
        var result = FormDialogTextResolver.ResolveForm(null!, "form1");

        Assert.Null(result);
    }

    #endregion

    #region TryResolveFormByArgument Tests

    [Fact]
    public void TryResolveFormByArgument_ByNumber_ReturnsCorrectForm()
    {
        var app = CreateApplicationWithForms(
            ("form1", "First Form"),
            ("form2", "Second Form"));
        var orderedForms = app.Forms.ToList();

        var success = FormDialogTextResolver.TryResolveFormByArgument(
            app, orderedForms, "en", "2", out var form, out var name);

        Assert.True(success);
        Assert.NotNull(form);
        Assert.Equal("form2", form.FormId);
        Assert.Equal("Second Form", name);
    }

    [Fact]
    public void TryResolveFormByArgument_ByName_ReturnsCorrectForm()
    {
        var app = CreateApplicationWithForms(
            ("form1", "First Form"),
            ("form2", "Second Form"));
        var orderedForms = app.Forms.ToList();

        var success = FormDialogTextResolver.TryResolveFormByArgument(
            app, orderedForms, "en", "second", out var form, out var name);

        Assert.True(success);
        Assert.NotNull(form);
        Assert.Equal("form2", form.FormId);
    }

    [Fact]
    public void TryResolveFormByArgument_InvalidNumber_ReturnsFalse()
    {
        var app = CreateApplicationWithForms(("form1", "First Form"));
        var orderedForms = app.Forms.ToList();

        var success = FormDialogTextResolver.TryResolveFormByArgument(
            app, orderedForms, "en", "5", out var form, out var name);

        Assert.False(success);
        Assert.Null(form);
    }

    [Fact]
    public void TryResolveFormByArgument_NoMatch_ReturnsFalse()
    {
        var app = CreateApplicationWithForms(("form1", "First Form"));
        var orderedForms = app.Forms.ToList();

        var success = FormDialogTextResolver.TryResolveFormByArgument(
            app, orderedForms, "en", "nonexistent", out var form, out var name);

        Assert.False(success);
        Assert.Null(form);
    }

    [Fact]
    public void TryResolveFormByArgument_PunctuationOnly_ReturnsFalse()
    {
        var app = CreateApplicationWithForms(("form1", "First Form"));
        var orderedForms = app.Forms.ToList();

        var success = FormDialogTextResolver.TryResolveFormByArgument(
            app, orderedForms, "en", "...", out var form, out var name);

        Assert.False(success);
        Assert.Null(form);
        Assert.Null(name);
    }

    #endregion

    #region Helper Methods

    private static FormContract CreateContractWithField(string variableId, LocalizedText label)
    {
        return new FormContract
        {
            ApplicationId = "test-app",
            FormId = FormId.From("test-form"),
            ContractVersion = "1.0.0",
            DefaultLanguage = "en",
            Fields = [new FieldDefinition { FieldId = FieldId.From(variableId), VariableId = VariableId.From(variableId), Label = label }],
            FieldGroups = [new FieldGroupDefinition { FieldGroupId = FieldGroupId.From("main") }],
            Variables = [new VariableDefinition { VariableId = VariableId.From(variableId) }]
        };
    }

    private static ApplicationDescriptor CreateApplication(string appId, string? defaultLanguage = null)
    {
        return new ApplicationDescriptor
        {
            ApplicationId = appId,
            DefaultLanguage = defaultLanguage ?? "en",
            SupportedLanguages = [defaultLanguage ?? "en", "de"],
            Forms = []
        };
    }

    private static ApplicationDescriptor CreateApplicationWithForm(string formId, LocalizedText displayName)
    {
        return new ApplicationDescriptor
        {
            ApplicationId = "test-app",
            DefaultLanguage = "en",
            SupportedLanguages = ["de", "en"],
            Forms = [new FormDescriptor { FormId = FormId.From(formId), DisplayName = displayName }]
        };
    }

    private static ApplicationDescriptor CreateApplicationWithForms(params (string formId, string name)[] forms)
    {
        return new ApplicationDescriptor
        {
            ApplicationId = "test-app",
            DefaultLanguage = "en",
            SupportedLanguages = ["en"],
            Forms = forms.Select(f => new FormDescriptor
            {
                FormId = FormId.From(f.formId),
                DisplayName = new LocalizedText { ["en"] = f.name }
            }).ToList()
        };
    }

    private static FormContract CreateContract(string appId, string formId, string version)
    {
        return new FormContract
        {
            ApplicationId = appId,
            FormId = string.IsNullOrWhiteSpace(formId) ? default : FormId.From(formId),
            ContractVersion = version,
            DefaultLanguage = "en",
            Fields = [],
            FieldGroups = [],
            Variables = []
        };
    }

    #endregion
}


