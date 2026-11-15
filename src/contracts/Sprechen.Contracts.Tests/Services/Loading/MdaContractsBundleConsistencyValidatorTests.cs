using Sprechen.Contracts.Services;
using Sprechen.Contracts.Services.Loading;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Xunit;

namespace Sprechen.Contracts.Tests.Services.Loading;

public sealed class MdaContractsBundleConsistencyValidatorTests
{
    [Fact]
    public void Validate_WhenEmbeddedReferencesAreConsistent_ReturnsBundle()
    {
        var (schemas, references) = LoadEmbeddedPipeline();

        var bundle = new MdaContractsBundleConsistencyValidator().Validate(schemas, references);

        Assert.NotEmpty(bundle.FormsCatalog.Content);
        Assert.True(bundle.ApplicationSelectionDialogsByKey.ContainsKey(new SelectionDialogKey("sample.application-selection-dialog", "5")));
        Assert.False(bundle.ApplicationSelectionDialogsByKey.ContainsKey(new SelectionDialogKey("sample.application-selection-dialog", "2")));
        Assert.True(bundle.FormContractsByKey.ContainsKey(new FormContractKey("sample.simple", "two-groups", "1")));
    }

    [Fact]
    public void Validate_WhenCatalogReferencesPreviousSelectionDialogVersion_Throws()
    {
        var (schemas, references) = LoadEmbeddedPipeline();
        references.FormsCatalogDocument.SelectionDialog.ModelVersion = "2";

        var ex = Assert.Throws<InvalidOperationException>(
            () => new MdaContractsBundleConsistencyValidator().Validate(schemas, references));

        Assert.Contains(
            "FormsCatalog references missing ApplicationSelectionDialog (selectionDialogId='sample.application-selection-dialog', modelVersion='2')",
            ex.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_WhenUnreferencedHistoricalSelectionDialogIsPresent_Throws()
    {
        var (schemas, references) = LoadEmbeddedPipeline();
        var sourceKey = new SelectionDialogKey("sample.application-selection-dialog", "5");
        var extraKey = new SelectionDialogKey("sample.application-selection-dialog", "3");
        var artifacts = references.SelectionDialogs.Artifacts.ToDictionary(static pair => pair.Key, static pair => pair.Value);
        var models = references.SelectionDialogs.Models.ToDictionary(static pair => pair.Key, static pair => pair.Value);
        artifacts.Add(extraKey, artifacts[sourceKey]);
        models.Add(extraKey, models[sourceKey]);
        references = references with
        {
            SelectionDialogs = new ArtifactSet<SelectionDialogKey, ApplicationSelectionDialog>(artifacts, models)
        };

        var ex = Assert.Throws<InvalidOperationException>(
            () => new MdaContractsBundleConsistencyValidator().Validate(schemas, references));

        Assert.Contains("modelVersion='3'", ex.Message, StringComparison.Ordinal);
        Assert.Contains("not referenced", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplicationSelectionDialogValidator_WhenCatalogReferencesMissingSelectionDialog_Throws()
    {
        var (_, references) = LoadEmbeddedPipeline();
        references = references with
        {
            SelectionDialogs = new ArtifactSet<SelectionDialogKey, ApplicationSelectionDialog>(
                new Dictionary<SelectionDialogKey, MdaJsonArtifact>(),
                new Dictionary<SelectionDialogKey, ApplicationSelectionDialog>())
        };

        var ex = Assert.Throws<InvalidOperationException>(
            () => new ApplicationSelectionDialogBundleConsistencyValidator().Validate(references.FormsCatalogDocument, references));

        Assert.Contains("FormsCatalog references missing ApplicationSelectionDialog", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_WhenCatalogReferencesMissingFormContract_Throws()
    {
        var (schemas, references) = LoadEmbeddedPipeline();
        var contractKey = MdaArtifactKeyFactory.CreateContractKey("sample.simple", "two-groups", "1");
        var contractArtifacts = references.FormContracts.Artifacts.ToDictionary(static pair => pair.Key, static pair => pair.Value);
        var contractModels = references.FormContracts.Models.ToDictionary(static pair => pair.Key, static pair => pair.Value);
        contractArtifacts.Remove(contractKey);
        contractModels.Remove(contractKey);
        references = references with
        {
            FormContracts = new ArtifactSet<FormContractKey, FormContractDocument>(
                contractArtifacts,
                contractModels)
        };

        var ex = Assert.Throws<InvalidOperationException>(
            () => new MdaContractsBundleConsistencyValidator().Validate(schemas, references));

        Assert.Contains("Catalog references missing FormContract", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FormContractValidator_WhenLoadedContractIsNotCatalogReferenced_Throws()
    {
        var (_, references) = LoadEmbeddedPipeline();

        var ex = Assert.Throws<InvalidOperationException>(
            () => new FormContractBundleConsistencyValidator().Validate(new HashSet<FormContractKey>(), references));

        Assert.Contains("Runtime bundle contains FormContract not referenced by FormsCatalog", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PresenceValidator_WhenArtifactIsNotReferenced_Throws()
    {
        var (_, references) = LoadEmbeddedPipeline();

        var ex = Assert.Throws<InvalidOperationException>(
            () => new MdaReferenceArtifactPresenceValidator().EnsureNoUnreferencedArtifacts(
                references.FieldDefinitions.Artifacts.Keys,
                Array.Empty<FieldDefinitionsKey>(),
                "FieldDefinitions",
                key => $"fieldDefinitionsId='{key.FieldDefinitionsId}', catalogVersion='{key.CatalogVersion}'"));

        Assert.Contains("Runtime bundle contains FieldDefinitions not referenced", ex.Message, StringComparison.Ordinal);
    }

    private static (MdaSchemaBundle Schemas, MdaReferenceArtifacts References) LoadEmbeddedPipeline()
    {
        var artifactReader = new MdaArtifactReader();
        var schemas = new MdaSchemaBundleFactory(artifactReader).Create();
        var references = new MdaReferenceArtifactLoader(artifactReader).Load(schemas);

        return (schemas, references);
    }
}
