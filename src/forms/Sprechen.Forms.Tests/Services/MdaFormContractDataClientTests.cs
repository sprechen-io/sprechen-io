using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Sprechen.Contracts.ApiClient;
using Sprechen.Forms.Services;
using Sprechen.Forms.Tests.TestDoubles;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Fields;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormVoice;
using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.Variables;
using Xunit;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Forms.Tests.Services;

public sealed class MdaFormContractDataClientTests
{
    [Fact]
    public async Task GetFormContractAsync_WhenCalledTwice_CachesResolvedContractAndReferencedCatalogs()
    {
        var contractDocument = CreateContractDocument(applicationId: "sample.profile", formId: "contact demo");
        var fieldCatalog = CreateFieldDefinitions(
            fieldDefinitionsId: contractDocument.FieldDefinitions.FieldDefinitionsId,
            catalogVersion: contractDocument.FieldDefinitions.CatalogVersion,
            fieldDefinitionId: contractDocument.Fields.Single().FieldDefinitionId,
            strictLocalization: false,
            label: new LocalizedText { ["de"] = "E-Mail" });
        var variableCatalog = CreateVariableDefinitions(
            variableDefinitionsId: contractDocument.VariableDefinitions.VariableDefinitionsId,
            catalogVersion: contractDocument.VariableDefinitions.CatalogVersion,
            variableDefinitionId: contractDocument.Variables.Single().VariableDefinitionId,
            strictLocalization: false,
            name: "email",
            valueTypeJson: """{"kind":"string","formatHint":"email"}""");

        var contractJson = JsonSerializer.Serialize(contractDocument);
        var fieldCatalogJson = JsonSerializer.Serialize(fieldCatalog);
        var variableCatalogJson = JsonSerializer.Serialize(variableCatalog);
        var formCalls = 0;
        var fieldCatalogCalls = 0;
        var variableCatalogCalls = 0;

        var handler = new DelegateContractsHttpMessageHandler(
            _ => throw new InvalidOperationException("Catalog should not be requested."),
            (_, _, _, _) =>
            {
                formCalls++;
                return Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(contractJson)));
            },
            (_, _, _) =>
            {
                fieldCatalogCalls++;
                return Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(fieldCatalogJson)));
            },
            (_, _, _) =>
            {
                variableCatalogCalls++;
                return Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(variableCatalogJson)));
            },
            (_, _, _) => throw new InvalidOperationException("Selection dialog model should not be requested."),
            (_, _, _) => throw new InvalidOperationException("Selection dialog profile should not be requested."),
            _ => throw new InvalidOperationException("Schema should not be requested."),
            _ => throw new InvalidOperationException("Schema should not be requested."),
            _ => throw new InvalidOperationException("Schema should not be requested."),
            _ => throw new InvalidOperationException("Schema should not be requested."),
            _ => throw new InvalidOperationException("Schema should not be requested."),
            _ => throw new InvalidOperationException("Schema should not be requested."));

        using var provider = CreateContractsApiClientProvider(handler);
        using var scope = provider.CreateScope();
        var client = new MdaFormContractDataClient(scope.ServiceProvider.GetRequiredService<MdaFormContractTypedClient>());

        var first = await client.GetFormContractAsync("sample.profile", "contact demo", "1", CancellationToken.None);
        var second = await client.GetFormContractAsync("sample.profile", "contact demo", "1", CancellationToken.None);

        Assert.Same(first, second);
        Assert.Equal(1, formCalls);
        Assert.Equal(1, fieldCatalogCalls);
        Assert.Equal(1, variableCatalogCalls);
        Assert.Equal("email", first.Variables.Single().Name);
        Assert.Equal("email", first.Fields.Single().VariableId);
    }

    [Fact]
    public async Task GetFormContractAsync_WhenFieldDefinitionsStrictLocalizationMissing_ThrowsFormContractLoadFailedException()
    {
        var contractDocument = CreateStrictContractDocument(applicationId: "sample.profile", formId: "contact demo");
        var fieldCatalog = CreateFieldDefinitions(
            fieldDefinitionsId: contractDocument.FieldDefinitions.FieldDefinitionsId,
            catalogVersion: contractDocument.FieldDefinitions.CatalogVersion,
            fieldDefinitionId: contractDocument.Fields.Single().FieldDefinitionId,
            strictLocalization: true,
            label: new LocalizedText { ["de"] = "Name" },
            supportedLanguages: ["de", "fr"]);
        var variableCatalog = CreateVariableDefinitions(
            variableDefinitionsId: contractDocument.VariableDefinitions.VariableDefinitionsId,
            catalogVersion: contractDocument.VariableDefinitions.CatalogVersion,
            variableDefinitionId: contractDocument.Variables.Single().VariableDefinitionId,
            strictLocalization: true,
            name: "name",
            valueTypeJson: """{"kind":"string"}""",
            supportedLanguages: ["de", "fr"],
            description: new LocalizedText { ["de"] = "Name", ["fr"] = "Nom" });

        var handler = new DelegateContractsHttpMessageHandler(
            _ => throw new InvalidOperationException("Catalog should not be requested."),
            (_, _, _, _) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(contractDocument)))),
            (_, _, _) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(fieldCatalog)))),
            (_, _, _) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(variableCatalog)))),
            (_, _, _) => throw new InvalidOperationException("Selection dialog model should not be requested."),
            (_, _, _) => throw new InvalidOperationException("Selection dialog profile should not be requested."),
            _ => throw new InvalidOperationException("Schema should not be requested."),
            _ => throw new InvalidOperationException("Schema should not be requested."),
            _ => throw new InvalidOperationException("Schema should not be requested."),
            _ => throw new InvalidOperationException("Schema should not be requested."),
            _ => throw new InvalidOperationException("Schema should not be requested."),
            _ => throw new InvalidOperationException("Schema should not be requested."));

        using var provider = CreateContractsApiClientProvider(handler);
        using var scope = provider.CreateScope();
        var client = new MdaFormContractDataClient(scope.ServiceProvider.GetRequiredService<MdaFormContractTypedClient>());

        var exception = await Assert.ThrowsAsync<FormContractLoadFailedException>(
            () => client.GetFormContractAsync("sample.profile", "contact demo", "1", CancellationToken.None));

        Assert.Contains(PreflightCodes.ErrorLocalizationMissing, exception.Issues.Select(issue => issue.Code));
    }

    [Fact]
    public async Task GetFormContractAsync_WhenDocumentReferencesFormVoiceProfile_LoadsAndResolvesProfile()
    {
        var contractDocument = CreateContractDocument(applicationId: "sample.profile", formId: "contact demo");
        contractDocument.Projections = new ContractProjectionsDocument
        {
            Voice = new VoiceProjectionDocument
            {
                FormVoiceProfile = new FormVoiceProfileReference
                {
                    FormVoiceProfileId = "sample.form-voice.default",
                    ProfileVersion = "1"
                }
            }
        };

        var fieldCatalog = CreateFieldDefinitions(
            fieldDefinitionsId: contractDocument.FieldDefinitions.FieldDefinitionsId,
            catalogVersion: contractDocument.FieldDefinitions.CatalogVersion,
            fieldDefinitionId: contractDocument.Fields.Single().FieldDefinitionId,
            strictLocalization: false,
            label: new LocalizedText { ["de"] = "E-Mail" });
        var variableCatalog = CreateVariableDefinitions(
            variableDefinitionsId: contractDocument.VariableDefinitions.VariableDefinitionsId,
            catalogVersion: contractDocument.VariableDefinitions.CatalogVersion,
            variableDefinitionId: contractDocument.Variables.Single().VariableDefinitionId,
            strictLocalization: false,
            name: "email",
            valueTypeJson: """{"kind":"string","formatHint":"email"}""");
        var formVoiceProfile = new FormVoiceProfile
        {
            SchemaVersion = "1",
            ProfileVersion = "1",
            FormVoiceProfileId = "sample.form-voice.default",
            AllowBargeIn = false,
            SpeechRate = 1.0
        };

        var handler = new DelegateContractsHttpMessageHandler(
            _ => throw new InvalidOperationException("Catalog should not be requested."),
            (_, _, _, _) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(contractDocument)))),
            (_, _, _) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(fieldCatalog)))),
            (_, _, _) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(variableCatalog)))),
            (_, _, _) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(formVoiceProfile)))),
            (_, _, _) => throw new InvalidOperationException("Selection dialog model should not be requested."),
            (_, _, _) => throw new InvalidOperationException("Selection dialog profile should not be requested."),
            _ => throw new InvalidOperationException("Schema should not be requested."),
            _ => throw new InvalidOperationException("Schema should not be requested."),
            _ => throw new InvalidOperationException("Schema should not be requested."),
            _ => throw new InvalidOperationException("Schema should not be requested."),
            _ => throw new InvalidOperationException("Schema should not be requested."),
            _ => throw new InvalidOperationException("Schema should not be requested."),
            _ => throw new InvalidOperationException("Schema should not be requested."));

        using var provider = CreateContractsApiClientProvider(handler);
        using var scope = provider.CreateScope();
        var client = new MdaFormContractDataClient(scope.ServiceProvider.GetRequiredService<MdaFormContractTypedClient>());

        var contract = await client.GetFormContractAsync("sample.profile", "contact demo", "1", CancellationToken.None);

        Assert.NotNull(contract.Projections?.Voice?.FormVoiceProfile);
        Assert.Equal("sample.form-voice.default", contract.Projections!.Voice!.FormVoiceProfile!.FormVoiceProfileId);
        Assert.Equal(1.0, contract.Projections.Voice.FormVoiceProfile.SpeechRate);
        Assert.False(contract.Projections.Voice.FormVoiceProfile.AllowBargeIn.GetValueOrDefault());
    }

    [Fact]
    public async Task GetFormContractAsync_WhenPatternRegexInvalid_ThrowsFormContractLoadFailedException()
    {
        var contractDocument = CreateContractDocument(applicationId: "sample.profile", formId: "contact demo");
        contractDocument.Constraints =
        [
            new ConstraintDefinition
            {
                ConstraintId = "invalid-pattern",
                Target = new ConstraintTargetRef { Kind = "field", FieldId = FieldId.From("email") },
                Severity = ConstraintSeverity.Error,
                Code = "E_PATTERN_INVALID",
                Message = new LocalizedText { ["de"] = "Ungueltiges Muster." },
                Kind = ConstraintKind.Pattern,
                AdditionalProperties = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    ["pattern"] = ParseJsonElement("\"(\"")
                }
            }
        ];

        var fieldCatalog = CreateFieldDefinitions(
            fieldDefinitionsId: contractDocument.FieldDefinitions.FieldDefinitionsId,
            catalogVersion: contractDocument.FieldDefinitions.CatalogVersion,
            fieldDefinitionId: contractDocument.Fields.Single().FieldDefinitionId,
            strictLocalization: false,
            label: new LocalizedText { ["de"] = "E-Mail" });
        var variableCatalog = CreateVariableDefinitions(
            variableDefinitionsId: contractDocument.VariableDefinitions.VariableDefinitionsId,
            catalogVersion: contractDocument.VariableDefinitions.CatalogVersion,
            variableDefinitionId: contractDocument.Variables.Single().VariableDefinitionId,
            strictLocalization: false,
            name: "email",
            valueTypeJson: """{"kind":"string"}""");

        var handler = new DelegateContractsHttpMessageHandler(
            _ => throw new InvalidOperationException("Catalog should not be requested."),
            (_, _, _, _) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(contractDocument)))),
            (_, _, _) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(fieldCatalog)))),
            (_, _, _) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(variableCatalog)))),
            (_, _, _) => throw new InvalidOperationException("Selection dialog model should not be requested."),
            (_, _, _) => throw new InvalidOperationException("Selection dialog profile should not be requested."),
            _ => throw new InvalidOperationException("Schema should not be requested."),
            _ => throw new InvalidOperationException("Schema should not be requested."),
            _ => throw new InvalidOperationException("Schema should not be requested."),
            _ => throw new InvalidOperationException("Schema should not be requested."),
            _ => throw new InvalidOperationException("Schema should not be requested."),
            _ => throw new InvalidOperationException("Schema should not be requested."));

        using var provider = CreateContractsApiClientProvider(handler);
        using var scope = provider.CreateScope();
        var client = new MdaFormContractDataClient(scope.ServiceProvider.GetRequiredService<MdaFormContractTypedClient>());

        var exception = await Assert.ThrowsAsync<FormContractLoadFailedException>(
            () => client.GetFormContractAsync("sample.profile", "contact demo", "1", CancellationToken.None));

        Assert.Contains(exception.Issues, issue =>
            issue.Code == PreflightCodes.ErrorSettingsInvalid
            && issue.Path == "$.constraints[0].pattern"
            && issue.Id == "invalid-pattern");
    }

    private static FormContractDocument CreateContractDocument(string applicationId, string formId)
    {
        return new FormContractDocument
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = applicationId,
            FormId = FormId.From(formId),
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            EntryFieldGroupId = FieldGroupId.From("entry"),
            StrictLocalization = false,
            FieldDefinitions = new FieldDefinitionsReference
            {
                FieldDefinitionsId = "sample.shared.fields.profile",
                CatalogVersion = "1"
            },
            VariableDefinitions = new VariableDefinitionsReference
            {
                VariableDefinitionsId = "sample.shared.variables.profile",
                CatalogVersion = "1"
            },
            Variables =
            [
                new FormVariableBindingDefinition
                {
                    VariableId = VariableId.From("email"),
                    VariableDefinitionId = "sample.profile.contact-demo.emailVar"
                }
            ],
            Fields =
            [
                new FormFieldBindingDefinition
                {
                    FieldId = FieldId.From("email"),
                    VariableId = VariableId.From("email"),
                    FieldDefinitionId = "sample.profile.contact-demo.emailField"
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("entry"),
                    Title = new LocalizedText { ["de"] = "Kontakt" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("email") }
                    ]
                }
            ],
            Constraints =
            [
                new ConstraintDefinition
                {
                    ConstraintId = "required-email",
                    Target = new ConstraintTargetRef { Kind = "field", FieldId = FieldId.From("email") },
                    Severity = ConstraintSeverity.Error,
                    Code = "E_REQUIRED",
                    Message = new LocalizedText { ["de"] = "E-Mail ist erforderlich." },
                    Kind = ConstraintKind.Required
                }
            ]
        };
    }

    private static FormContractDocument CreateStrictContractDocument(string applicationId, string formId)
    {
        return new FormContractDocument
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = applicationId,
            FormId = FormId.From(formId),
            SupportedLanguages = ["de", "fr"],
            DefaultLanguage = "de",
            EntryFieldGroupId = FieldGroupId.From("entry"),
            StrictLocalization = true,
            FieldDefinitions = new FieldDefinitionsReference
            {
                FieldDefinitionsId = "sample.shared.fields.strict-profile",
                CatalogVersion = "1"
            },
            VariableDefinitions = new VariableDefinitionsReference
            {
                VariableDefinitionsId = "sample.shared.variables.strict-profile",
                CatalogVersion = "1"
            },
            Variables =
            [
                new FormVariableBindingDefinition
                {
                    VariableId = VariableId.From("name"),
                    VariableDefinitionId = "sample.profile.contact-demo.nameVar"
                }
            ],
            Fields =
            [
                new FormFieldBindingDefinition
                {
                    FieldId = FieldId.From("name"),
                    VariableId = VariableId.From("name"),
                    FieldDefinitionId = "sample.profile.contact-demo.nameField"
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("entry"),
                    Title = new LocalizedText { ["de"] = "Adressdaten", ["fr"] = "Coordonnees" },
                    Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("name") }]
                }
            ]
        };
    }

    private static FieldDefinitions CreateFieldDefinitions(
        string fieldDefinitionsId,
        string catalogVersion,
        string fieldDefinitionId,
        bool strictLocalization,
        LocalizedText label,
        List<string>? supportedLanguages = null)
    {
        return new FieldDefinitions
        {
            SchemaVersion = "1",
            CatalogVersion = catalogVersion,
            FieldDefinitionsId = fieldDefinitionsId,
            SupportedLanguages = supportedLanguages ?? ["de"],
            DefaultLanguage = "de",
            StrictLocalization = strictLocalization,
            Definitions =
            [
                new ReusableFieldDefinition
                {
                    FieldDefinitionId = fieldDefinitionId,
                    Label = label,
                    Editability = FieldEditability.UserEditable
                }
            ]
        };
    }

    private static VariableDefinitions CreateVariableDefinitions(
        string variableDefinitionsId,
        string catalogVersion,
        string variableDefinitionId,
        bool strictLocalization,
        string name,
        string valueTypeJson,
        List<string>? supportedLanguages = null,
        LocalizedText? description = null)
    {
        return new VariableDefinitions
        {
            SchemaVersion = "1",
            CatalogVersion = catalogVersion,
            VariableDefinitionsId = variableDefinitionsId,
            SupportedLanguages = supportedLanguages ?? ["de"],
            DefaultLanguage = "de",
            StrictLocalization = strictLocalization,
            Definitions =
            [
                new ReusableVariableDefinition
                {
                    VariableDefinitionId = variableDefinitionId,
                    Name = name,
                    Description = description,
                    ValueType = ParseJsonElement(valueTypeJson)
                }
            ]
        };
    }

    private static JsonElement ParseJsonElement(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
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
