using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Xunit;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SharedVariableResolverTests
{
    #region OrderForms

    [Fact]
    public void OrderForms_SortsFormsWithSequenceFirst_ThenBySequenceValue_ThenById()
    {
        var application = new ApplicationDescriptor
        {
            ApplicationId = "app1",
            Forms =
            [
                new FormDescriptor { FormId = FormId.From("form-z"), Sequence = null },
                new FormDescriptor { FormId = FormId.From("form-a"), Sequence = 2 },
                new FormDescriptor { FormId = FormId.From("form-b"), Sequence = 1 },
                new FormDescriptor { FormId = FormId.From("form-c"), Sequence = null }
            ]
        };

        var result = new FormsCatalogFormOrder().OrderForms(application);

        Assert.Equal(4, result.Length);
        Assert.Equal("form-b", result[0].FormId);
        Assert.Equal("form-a", result[1].FormId); // Nach den Formularen mit Reihenfolgenwert folgen die übrigen nach Formular-ID.
        Assert.Equal("form-c", result[2].FormId);
        Assert.Equal("form-z", result[3].FormId);
    }

    [Fact]
    public void OrderForms_ExcludesNullAndEmptyFormIds()
    {
        var application = new ApplicationDescriptor
        {
            ApplicationId = "app1",
            Forms =
            [
                new FormDescriptor { FormId = FormId.From("valid") },
                new FormDescriptor { FormId = default },
                new FormDescriptor { FormId = default },
                new FormDescriptor { FormId = FormId.From("also-valid") }
            ]
        };

        var result = new FormsCatalogFormOrder().OrderForms(application);

        Assert.Equal(2, result.Length);
        Assert.Equal("also-valid", result[0].FormId);
        Assert.Equal("valid", result[1].FormId);
    }

    #endregion

    #region ApplySharedVariables

    [Fact]
    public void ApplySharedVariables_NoSharedVariables_ReturnsOriginalValues()
    {
        var application = new ApplicationDescriptor { ApplicationId = "app1" };
        var targetForm = new FormDescriptor { FormId = FormId.From("form1"), SharedVariables = null };
        var contract = CreateContract("var1");
        var session = SpeechAssistantApplicationSessionState.Empty;
        var initialValues = ImmutableDictionary<string, string>.Empty;

        var (values, adoptions) = CreateResolver().ApplySharedVariables(
            application, targetForm, contract, session, initialValues);

        Assert.Same(initialValues, values);
        Assert.Empty(adoptions);
    }

    [Fact]
    public void ApplySharedVariables_EmptySharedVariables_ReturnsOriginalValues()
    {
        var application = new ApplicationDescriptor { ApplicationId = "app1" };
        var targetForm = new FormDescriptor { FormId = FormId.From("form1"), SharedVariables = [] };
        var contract = CreateContract("var1");
        var session = SpeechAssistantApplicationSessionState.Empty;
        var initialValues = ImmutableDictionary<string, string>.Empty;

        var (values, adoptions) = CreateResolver().ApplySharedVariables(
            application, targetForm, contract, session, initialValues);

        Assert.Same(initialValues, values);
        Assert.Empty(adoptions);
    }

    [Fact]
    public void ApplySharedVariables_NoCompletedForms_ReturnsOriginalValues()
    {
        var application = new ApplicationDescriptor { ApplicationId = "app1" };
        var targetForm = new FormDescriptor { FormId = FormId.From("form1"), SharedVariables = ["shared-var"] };
        var contract = CreateContract("shared-var");
        var session = SpeechAssistantApplicationSessionState.Empty;
        var initialValues = ImmutableDictionary<string, string>.Empty;

        var (values, adoptions) = CreateResolver().ApplySharedVariables(
            application, targetForm, contract, session, initialValues);

        Assert.Same(initialValues, values);
        Assert.Empty(adoptions);
    }

    [Fact]
    public void ApplySharedVariables_AdoptsValueFromSubmittedForm()
    {
        var application = new ApplicationDescriptor
        {
            ApplicationId = "app1",
            Forms =
            [
                new FormDescriptor { FormId = FormId.From("form1"), Sequence = 1 },
                new FormDescriptor { FormId = FormId.From("form2"), Sequence = 2, SharedVariables = ["shared-var"] }
            ]
        };
        var targetForm = application.Forms[1];
        var contract = CreateContract("shared-var");
        var formSession = new SpeechAssistantFormSessionState(
            ImmutableDictionary<string, string>.Empty.Add("shared-var", "adopted-value"),
            IsSubmitted: true);
        var session = new SpeechAssistantApplicationSessionState(
            ImmutableDictionary<string, SpeechAssistantFormSessionState>.Empty.Add("form1", formSession));
        var initialValues = ImmutableDictionary<string, string>.Empty;

        var (values, adoptions) = CreateResolver().ApplySharedVariables(
            application, targetForm, contract, session, initialValues);

        Assert.Equal("adopted-value", values["shared-var"]);
        Assert.Single(adoptions);
        Assert.Equal("shared-var", adoptions[0].VariableId);
        Assert.Equal("form1", adoptions[0].SourceFormId);
    }

    [Fact]
    public void ApplySharedVariables_DoesNotOverwriteExistingValues()
    {
        var application = new ApplicationDescriptor
        {
            ApplicationId = "app1",
            Forms =
            [
                new FormDescriptor { FormId = FormId.From("form1"), Sequence = 1 },
                new FormDescriptor { FormId = FormId.From("form2"), Sequence = 2, SharedVariables = ["shared-var"] }
            ]
        };
        var targetForm = application.Forms[1];
        var contract = CreateContract("shared-var");
        var formSession = new SpeechAssistantFormSessionState(
            ImmutableDictionary<string, string>.Empty.Add("shared-var", "other-value"),
            IsSubmitted: true);
        var session = new SpeechAssistantApplicationSessionState(
            ImmutableDictionary<string, SpeechAssistantFormSessionState>.Empty.Add("form1", formSession));
        var initialValues = ImmutableDictionary<string, string>.Empty.Add("shared-var", "existing-value");

        var (values, adoptions) = CreateResolver().ApplySharedVariables(
            application, targetForm, contract, session, initialValues);

        Assert.Equal("existing-value", values["shared-var"]);
        Assert.Empty(adoptions);
    }

    [Fact]
    public void ApplySharedVariables_SkipsNotSubmittedForms()
    {
        var application = new ApplicationDescriptor
        {
            ApplicationId = "app1",
            Forms =
            [
                new FormDescriptor { FormId = FormId.From("form1"), Sequence = 1 },
                new FormDescriptor { FormId = FormId.From("form2"), Sequence = 2, SharedVariables = ["shared-var"] }
            ]
        };
        var targetForm = application.Forms[1];
        var contract = CreateContract("shared-var");
        var formSession = new SpeechAssistantFormSessionState(
            ImmutableDictionary<string, string>.Empty.Add("shared-var", "value"),
            IsSubmitted: false);
        var session = new SpeechAssistantApplicationSessionState(
            ImmutableDictionary<string, SpeechAssistantFormSessionState>.Empty.Add("form1", formSession));
        var initialValues = ImmutableDictionary<string, string>.Empty;

        var (values, adoptions) = CreateResolver().ApplySharedVariables(
            application, targetForm, contract, session, initialValues);

        Assert.Empty(values);
        Assert.Empty(adoptions);
    }

    #endregion

    #region IsVariableUsedInContract

    [Fact]
    public void IsVariableUsedInContract_VariableDefinitionExists_ReturnsTrue()
    {
        var contract = CreateContract("my-variable");

        var result = CreateResolver().IsVariableUsedInContract(contract, "my-variable");

        Assert.True(result);
    }

    [Fact]
    public void IsVariableUsedInContract_VariableNotFound_ReturnsFalse()
    {
        var contract = CreateContract("other-variable");

        var result = CreateResolver().IsVariableUsedInContract(contract, "nonexistent");

        Assert.False(result);
    }

    [Fact]
    public void IsVariableUsedInContract_RequiresExactOrdinalIdentity()
    {
        var contract = CreateContract("MyVariable");

        Assert.True(CreateResolver().IsVariableUsedInContract(contract, "MyVariable"));
        Assert.False(CreateResolver().IsVariableUsedInContract(contract, "MYVARIABLE"));
        Assert.False(CreateResolver().IsVariableUsedInContract(contract, " MyVariable "));
    }

    #endregion

    #region SharedVariableSourceFormOrder

    [Fact]
    public void SourceFormOrder_PrioritizesPrerequisitesBeforeRemainingForms()
    {
        var application = new ApplicationDescriptor
        {
            ApplicationId = "app1",
            Forms =
            [
                new FormDescriptor { FormId = FormId.From("profile"), Sequence = 1 },
                new FormDescriptor { FormId = FormId.From("billing"), Sequence = 2 },
                new FormDescriptor { FormId = FormId.From("target"), Sequence = 3, PrerequisiteForms = ["billing"] },
                new FormDescriptor { FormId = FormId.From("archive"), Sequence = 4 }
            ]
        };

        var sourceFormIds = new SharedVariableSourceFormOrder().Resolve(application, application.Forms[2]);

        Assert.Equal(["billing", "profile", "archive"], sourceFormIds.ToArray());
    }

    #endregion

    #region SharedVariableContractUsageResolver

    [Fact]
    public void ContractUsageResolver_WhenFieldUsesImplicitVariableId_ReturnsTrue()
    {
        var contract = new FormContract
        {
            ApplicationId = "app1",
            FormId = FormId.From("form1"),
            ContractVersion = "1.0.0",
            Variables = [],
            Fields = [new FieldDefinition { FieldId = FieldId.From("implicit-var") }],
            FieldGroups = []
        };

        var result = new SharedVariableContractUsageResolver().IsVariableUsedInContract(contract, "implicit-var");

        Assert.True(result);
    }

    #endregion

    private static SharedVariableResolver CreateResolver()
        => new();

    private static FormContract CreateContract(string variableId)
    {
        return new FormContract
        {
            ApplicationId = "app1",
            FormId = FormId.From("form1"),
            ContractVersion = "1.0.0",
            Variables = [new VariableDefinition { VariableId = VariableId.From(variableId) }],
            Fields = [],
            FieldGroups = []
        };
    }
}

