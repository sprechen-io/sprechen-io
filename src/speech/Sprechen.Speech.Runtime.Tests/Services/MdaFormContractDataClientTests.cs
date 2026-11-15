using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Sprechen.Contracts.ApiClient;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Fields;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormVoice;
using Sprechen.Mda.Contracts.V1.Variables;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class MdaFormContractDataClientTests
{
    [Fact]
    public async Task GetFormContractAsync_WhenInputsContainWhitespace_UsesNormalizedCacheKeyAndCallsApisOnce()
    {
        var contractDocument = CreateValidFormContractDocument("1.0.0");
        var fieldCatalog = CreateFieldDefinitions(
            fieldDefinitionsId: contractDocument.FieldDefinitions.FieldDefinitionsId,
            catalogVersion: contractDocument.FieldDefinitions.CatalogVersion,
            fieldDefinitionId: contractDocument.Fields.Single().FieldDefinitionId);
        var variableCatalog = CreateVariableDefinitions(
            variableDefinitionsId: contractDocument.VariableDefinitions.VariableDefinitionsId,
            catalogVersion: contractDocument.VariableDefinitions.CatalogVersion,
            variableDefinitionId: contractDocument.Variables.Single().VariableDefinitionId);

        var handler = new MappedContractsHttpMessageHandler();
        handler.Add("/contracts/app/form/1.0.0", () => SerializeToStream(contractDocument));
        handler.Add(
            $"/contracts/field-definitions/{contractDocument.FieldDefinitions.FieldDefinitionsId}/{contractDocument.FieldDefinitions.CatalogVersion}",
            () => SerializeToStream(fieldCatalog));
        handler.Add(
            $"/contracts/variable-definitions/{contractDocument.VariableDefinitions.VariableDefinitionsId}/{contractDocument.VariableDefinitions.CatalogVersion}",
            () => SerializeToStream(variableCatalog));

        using var provider = CreateContractsApiClientProvider(handler);
        using var scope = provider.CreateScope();
        var client = new MdaFormContractDataClient(scope.ServiceProvider.GetRequiredService<MdaFormContractTypedClient>());

        var first = await client.GetFormContractAsync(" app ", " form ", " 1.0.0 ", CancellationToken.None);
        var second = await client.GetFormContractAsync("app", "form", "1.0.0", CancellationToken.None);

        Assert.Same(first, second);
        Assert.Equal("app", first.ApplicationId);
        Assert.Equal("form", first.FormId);
        Assert.Equal("1.0.0", first.ContractVersion);
        Assert.Equal(contractDocument.Fields.Single().FieldDefinitionId, first.Fields.Single().FieldDefinitionId);
        Assert.Equal("value", first.Variables.Single().Name);

        Assert.Equal(1, handler.GetCallCount("/contracts/app/form/1.0.0"));
        Assert.Equal(
            1,
            handler.GetCallCount($"/contracts/field-definitions/{contractDocument.FieldDefinitions.FieldDefinitionsId}/{contractDocument.FieldDefinitions.CatalogVersion}"));
        Assert.Equal(
            1,
            handler.GetCallCount($"/contracts/variable-definitions/{contractDocument.VariableDefinitions.VariableDefinitionsId}/{contractDocument.VariableDefinitions.CatalogVersion}"));
    }

    [Fact]
    public async Task GetFormContractAsync_WhenDifferentVersions_UsesSeparateCaches()
    {
        var contractV1 = CreateValidFormContractDocument("1.0.0");
        var contractV2 = CreateValidFormContractDocument("2.0.0");
        var fieldCatalogV1 = CreateFieldDefinitions(
            fieldDefinitionsId: contractV1.FieldDefinitions.FieldDefinitionsId,
            catalogVersion: contractV1.FieldDefinitions.CatalogVersion,
            fieldDefinitionId: contractV1.Fields.Single().FieldDefinitionId);
        var fieldCatalogV2 = CreateFieldDefinitions(
            fieldDefinitionsId: contractV2.FieldDefinitions.FieldDefinitionsId,
            catalogVersion: contractV2.FieldDefinitions.CatalogVersion,
            fieldDefinitionId: contractV2.Fields.Single().FieldDefinitionId);
        var variableCatalogV1 = CreateVariableDefinitions(
            variableDefinitionsId: contractV1.VariableDefinitions.VariableDefinitionsId,
            catalogVersion: contractV1.VariableDefinitions.CatalogVersion,
            variableDefinitionId: contractV1.Variables.Single().VariableDefinitionId);
        var variableCatalogV2 = CreateVariableDefinitions(
            variableDefinitionsId: contractV2.VariableDefinitions.VariableDefinitionsId,
            catalogVersion: contractV2.VariableDefinitions.CatalogVersion,
            variableDefinitionId: contractV2.Variables.Single().VariableDefinitionId);

        var handler = new MappedContractsHttpMessageHandler();
        handler.Add("/contracts/app/form/1.0.0", () => SerializeToStream(contractV1));
        handler.Add(
            $"/contracts/field-definitions/{contractV1.FieldDefinitions.FieldDefinitionsId}/{contractV1.FieldDefinitions.CatalogVersion}",
            () => SerializeToStream(fieldCatalogV1));
        handler.Add(
            $"/contracts/variable-definitions/{contractV1.VariableDefinitions.VariableDefinitionsId}/{contractV1.VariableDefinitions.CatalogVersion}",
            () => SerializeToStream(variableCatalogV1));
        handler.Add("/contracts/app/form/2.0.0", () => SerializeToStream(contractV2));
        handler.Add(
            $"/contracts/field-definitions/{contractV2.FieldDefinitions.FieldDefinitionsId}/{contractV2.FieldDefinitions.CatalogVersion}",
            () => SerializeToStream(fieldCatalogV2));
        handler.Add(
            $"/contracts/variable-definitions/{contractV2.VariableDefinitions.VariableDefinitionsId}/{contractV2.VariableDefinitions.CatalogVersion}",
            () => SerializeToStream(variableCatalogV2));

        using var provider = CreateContractsApiClientProvider(handler);
        using var scope = provider.CreateScope();
        var client = new MdaFormContractDataClient(scope.ServiceProvider.GetRequiredService<MdaFormContractTypedClient>());

        var first = await client.GetFormContractAsync("app", "form", "1.0.0", CancellationToken.None);
        var second = await client.GetFormContractAsync("app", "form", "2.0.0", CancellationToken.None);

        Assert.NotSame(first, second);
        Assert.Equal("1.0.0", first.ContractVersion);
        Assert.Equal("2.0.0", second.ContractVersion);

        Assert.Equal(1, handler.GetCallCount("/contracts/app/form/1.0.0"));
        Assert.Equal(
            1,
            handler.GetCallCount($"/contracts/field-definitions/{contractV1.FieldDefinitions.FieldDefinitionsId}/{contractV1.FieldDefinitions.CatalogVersion}"));
        Assert.Equal(
            1,
            handler.GetCallCount($"/contracts/variable-definitions/{contractV1.VariableDefinitions.VariableDefinitionsId}/{contractV1.VariableDefinitions.CatalogVersion}"));
        Assert.Equal(1, handler.GetCallCount("/contracts/app/form/2.0.0"));
        Assert.Equal(
            1,
            handler.GetCallCount($"/contracts/field-definitions/{contractV2.FieldDefinitions.FieldDefinitionsId}/{contractV2.FieldDefinitions.CatalogVersion}"));
        Assert.Equal(
            1,
            handler.GetCallCount($"/contracts/variable-definitions/{contractV2.VariableDefinitions.VariableDefinitionsId}/{contractV2.VariableDefinitions.CatalogVersion}"));
    }

    [Fact]
    public async Task GetFormContractAsync_WhenDocumentReferencesFormVoiceProfile_LoadsAndResolvesProfile()
    {
        var contractDocument = CreateValidFormContractDocument("1.0.0");
        contractDocument.Projections = new ContractProjectionsDocument
        {
            Voice = new VoiceProjectionDocument
            {
                FormVoiceProfile = new FormVoiceProfileReference
                {
                    FormVoiceProfileId = "app.form.voice.default",
                    ProfileVersion = "1.0.0"
                }
            }
        };

        var fieldCatalog = CreateFieldDefinitions(
            fieldDefinitionsId: contractDocument.FieldDefinitions.FieldDefinitionsId,
            catalogVersion: contractDocument.FieldDefinitions.CatalogVersion,
            fieldDefinitionId: contractDocument.Fields.Single().FieldDefinitionId);
        var variableCatalog = CreateVariableDefinitions(
            variableDefinitionsId: contractDocument.VariableDefinitions.VariableDefinitionsId,
            catalogVersion: contractDocument.VariableDefinitions.CatalogVersion,
            variableDefinitionId: contractDocument.Variables.Single().VariableDefinitionId);
        var formVoiceProfile = new FormVoiceProfile
        {
            SchemaVersion = "1",
            ProfileVersion = "1.0.0",
            FormVoiceProfileId = "app.form.voice.default",
            AllowBargeIn = true,
            NoInputTimeoutMs = 7000,
            SpeechRate = 1.2
        };

        var handler = new MappedContractsHttpMessageHandler();
        handler.Add("/contracts/app/form/1.0.0", () => SerializeToStream(contractDocument));
        handler.Add(
            $"/contracts/field-definitions/{contractDocument.FieldDefinitions.FieldDefinitionsId}/{contractDocument.FieldDefinitions.CatalogVersion}",
            () => SerializeToStream(fieldCatalog));
        handler.Add(
            $"/contracts/variable-definitions/{contractDocument.VariableDefinitions.VariableDefinitionsId}/{contractDocument.VariableDefinitions.CatalogVersion}",
            () => SerializeToStream(variableCatalog));
        handler.Add("/contracts/form-voice-profiles/app.form.voice.default/1.0.0", () => SerializeToStream(formVoiceProfile));

        using var provider = CreateContractsApiClientProvider(handler);
        using var scope = provider.CreateScope();
        var client = new MdaFormContractDataClient(scope.ServiceProvider.GetRequiredService<MdaFormContractTypedClient>());

        var contract = await client.GetFormContractAsync("app", "form", "1.0.0", CancellationToken.None);

        Assert.NotNull(contract.Projections?.Voice?.FormVoiceProfile);
        Assert.Equal("app.form.voice.default", contract.Projections!.Voice!.FormVoiceProfile!.FormVoiceProfileId);
        Assert.True(contract.Projections.Voice.FormVoiceProfile.AllowBargeIn);
        Assert.Equal(7000, contract.Projections.Voice.FormVoiceProfile.NoInputTimeoutMs);
        Assert.Equal(1.2, contract.Projections.Voice.FormVoiceProfile.SpeechRate);

        Assert.Equal(1, handler.GetCallCount("/contracts/form-voice-profiles/app.form.voice.default/1.0.0"));
    }

    private static FormContractDocument CreateValidFormContractDocument(string contractVersion)
    {
        return new FormContractDocument
        {
            SchemaVersion = "1",
            ContractVersion = contractVersion,
            ApplicationId = "app",
            FormId = FormId.From("form"),
            DisplayName = new LocalizedText { ["de"] = "Form" },
            Description = new LocalizedText { ["de"] = "Form" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            EntryFieldGroupId = FieldGroupId.From("start"),
            StrictLocalization = false,
            FieldDefinitions = new FieldDefinitionsReference
            {
                FieldDefinitionsId = $"app.form.fields.{contractVersion.Replace('.', '-')}",
                CatalogVersion = contractVersion
            },
            VariableDefinitions = new VariableDefinitionsReference
            {
                VariableDefinitionsId = $"app.form.variables.{contractVersion.Replace('.', '-')}",
                CatalogVersion = contractVersion
            },
            Variables =
            [
                new FormVariableBindingDefinition
                {
                    VariableId = VariableId.From("value"),
                    VariableDefinitionId = $"app.form.variables.{contractVersion.Replace('.', '-')}.value"
                }
            ],
            Fields =
            [
                new FormFieldBindingDefinition
                {
                    FieldId = FieldId.From("valueField"),
                    VariableId = VariableId.From("value"),
                    FieldDefinitionId = $"app.form.fields.{contractVersion.Replace('.', '-')}.valueField"
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("start"),
                    Title = new LocalizedText { ["de"] = "Start" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("valueField") }
                    ]
                }
            ]
        };
    }

    private static FieldDefinitions CreateFieldDefinitions(string fieldDefinitionsId, string catalogVersion, string fieldDefinitionId)
    {
        return new FieldDefinitions
        {
            SchemaVersion = "1",
            CatalogVersion = catalogVersion,
            FieldDefinitionsId = fieldDefinitionsId,
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = false,
            Definitions =
            [
                new ReusableFieldDefinition
                {
                    FieldDefinitionId = fieldDefinitionId,
                    Label = new LocalizedText { ["de"] = "Wert" },
                    Editability = FieldEditability.UserEditable
                }
            ]
        };
    }

    private static VariableDefinitions CreateVariableDefinitions(string variableDefinitionsId, string catalogVersion, string variableDefinitionId)
    {
        return new VariableDefinitions
        {
            SchemaVersion = "1",
            CatalogVersion = catalogVersion,
            VariableDefinitionsId = variableDefinitionsId,
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = false,
            Definitions =
            [
                new ReusableVariableDefinition
                {
                    VariableDefinitionId = variableDefinitionId,
                    Name = "value",
                    ValueType = CreateValueType("string")
                }
            ]
        };
    }

    private static JsonElement CreateValueType(string kind)
    {
        using var doc = JsonDocument.Parse($"{{\"kind\":\"{kind}\"}}");
        return doc.RootElement.Clone();
    }

    private static MemoryStream SerializeToStream<T>(T value)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)));
    }

    private static ServiceProvider CreateContractsApiClientProvider(HttpMessageHandler handler)
    {
        var services = new ServiceCollection();
        services.AddContractsApiClient(
            "ContractsHttp",
            (_, httpClient) => httpClient.BaseAddress = new Uri("https://contracts.test", UriKind.Absolute));
        services
            .AddHttpClient("ContractsHttp")
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        return services.BuildServiceProvider(validateScopes: true);
    }
}


