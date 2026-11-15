using System.Text.Json;
using Sprechen.Contracts.Services;
using Sprechen.Contracts.Services.Loading;
using Sprechen.Mda.Contracts.V1.Catalog;
using Xunit;

namespace Sprechen.Contracts.Tests.Services;

public sealed class MdaContractsStoreTests
{
    [Fact]
    public void Ctor_WhenEmbeddedArtifactsArePresent_LoadsCatalogAndContracts()
    {
        var store = CreateStore();

        Assert.NotEmpty(store.FormsCatalog.Content);
        Assert.NotEmpty(store.FormsCatalogSchema.Content);
        Assert.NotEmpty(store.LocalizationPrimitivesSchema.Content);
        Assert.NotEmpty(store.ExpressionLanguageSchema.Content);
        Assert.NotEmpty(store.ConstraintLanguageSchema.Content);
        Assert.NotEmpty(store.PrivacyPolicySchema.Content);
        Assert.NotEmpty(store.FieldDefinitionsSchema.Content);
        Assert.NotEmpty(store.VariableDefinitionsSchema.Content);
        Assert.NotEmpty(store.FormVoiceProfileSchema.Content);
        Assert.NotEmpty(store.ApplicationSelectionDialogSchema.Content);
        Assert.NotEmpty(store.ApplicationSelectionDialogProfileSchema.Content);
        Assert.NotEmpty(store.FormContractSchema.Content);

        Assert.True(store.TryGetFieldDefinitions("sample.simple.fields.multilingual", "1", out var fieldCatalogArtifact));
        Assert.NotEmpty(fieldCatalogArtifact.Content);

        Assert.True(store.TryGetVariableDefinitions("sample.simple.variables.multilingual", "1", out var variableCatalogArtifact));
        Assert.NotEmpty(variableCatalogArtifact.Content);

        Assert.True(store.TryGetApplicationSelectionDialog("sample.application-selection-dialog", "5", out var selectionDialogArtifact));
        Assert.NotEmpty(selectionDialogArtifact.Content);
        Assert.False(store.TryGetApplicationSelectionDialog("sample.application-selection-dialog", "2", out _));

        var selectionDialog = JsonSerializer.Deserialize<ApplicationSelectionDialog>(selectionDialogArtifact.Content);
        Assert.NotNull(selectionDialog);
        Assert.Equal("5", selectionDialog.ModelVersion);
        Assert.Single(selectionDialog.Intents, intent => intent.IntentId == "openCandidate");

        Assert.True(store.TryGetApplicationSelectionDialogProfile("sample.application-selection-dialog.default", "1", out var selectionDialogProfileArtifact));
        Assert.NotEmpty(selectionDialogProfileArtifact.Content);

        Assert.True(store.TryGetFormContract("sample.simple", "two-groups", "1", out var artifact));
        Assert.NotEmpty(artifact.Content);
    }

    [Fact]
    public void TryGetFormContract_WhenContractIsNotDiscoverable_ReturnsFalse()
    {
        var store = CreateStore();

        Assert.False(store.TryGetFormContract("nonexistent.app", "missing-form", "1", out _));
    }

    [Fact]
    public void TryGetFieldDefinitions_WhenCatalogIsNotDiscoverable_ReturnsFalse()
    {
        var store = CreateStore();

        Assert.False(store.TryGetFieldDefinitions("sample.voice-complex.fields", "1", out _));
    }

    [Fact]
    public void TryGetVariableDefinitions_WhenCatalogIsNotDiscoverable_ReturnsFalse()
    {
        var store = CreateStore();

        Assert.False(store.TryGetVariableDefinitions("sample.voice-complex.variables", "1", out _));
    }

    [Fact]
    public void TryGetApplicationSelectionDialog_WhenDialogIsNotDiscoverable_ReturnsFalse()
    {
        var store = CreateStore();

        Assert.False(store.TryGetApplicationSelectionDialog("sample.unknown.selection-dialog", "1", out _));
    }

    [Fact]
    public void TryGetFormVoiceProfile_WhenProfileIsNotDiscoverable_ReturnsFalse()
    {
        var store = CreateStore();

        Assert.False(store.TryGetFormVoiceProfile("sample.form-voice.unknown", "1", out _));
    }

    [Fact]
    public void TryGetApplicationSelectionDialogProfile_WhenProfileIsNotDiscoverable_ReturnsFalse()
    {
        var store = CreateStore();

        Assert.False(store.TryGetApplicationSelectionDialogProfile("sample.unknown.application-selection-dialog-profile", "1", out _));
    }

    [Fact]
    public void TryGetFormContract_NonExistentForm_ReturnsFalse()
    {
        var store = CreateStore();

        Assert.False(store.TryGetFormContract("sample.simple", "nonexistent", "1", out _));
    }

    [Fact]
    public void TryGetFormContract_NonExistentVersion_ReturnsFalse()
    {
        var store = CreateStore();

        Assert.False(store.TryGetFormContract("sample.simple", "two-groups", "999", out _));
    }

    [Fact]
    public void ArtifactIndex_WhenLookupArgumentsHaveWhitespace_UsesCanonicalArtifactKeys()
    {
        var index = new MdaContractsStoreArtifactIndex(CreateBundle());

        Assert.True(index.TryGetFormContract(" sample.simple ", " two-groups ", " 1 ", out var artifact));
        Assert.NotEmpty(artifact.Content);
    }

    private static MdaContractsStore CreateStore()
    {
        return new MdaContractsStore(CreateBundle());
    }

    private static MdaContractsBundle CreateBundle()
    {
        var artifactReader = new MdaArtifactReader();
        var schemas = new MdaSchemaBundleFactory(artifactReader).Create();
        var references = new MdaReferenceArtifactLoader(artifactReader).Load(schemas);

        return new MdaContractsBundleConsistencyValidator().Validate(schemas, references);
    }
}
