using Sprechen.ServiceDefaults;
using Sprechen.Contracts.ApiClient;
using Sprechen.Contracts.Services;
using Sprechen.Contracts.Services.Loading;

namespace Sprechen.Contracts.Extensions;

/// <summary>
/// Registriert Modellprüfung und HTTP-Routen des Vertragsdienstes, damit veröffentlichte Artefakte vor der ersten Anfrage geprüft werden.
/// </summary>
internal static class HostExtensions
{
    public static void AddSprechenContracts(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.SetupKestrel();

        builder.AddSprechenServiceDefaults();

        builder.Services.AddSingleton<MdaArtifactReader>();
        builder.Services.AddSingleton<MdaSchemaBundleFactory>();
        builder.Services.AddSingleton<MdaReferenceArtifactLoader>();
        builder.Services.AddSingleton<MdaContractsBundleConsistencyValidator>();
        builder.Services.AddSingleton(sp =>
        {
            var schemas = sp.GetRequiredService<MdaSchemaBundleFactory>().Create();
            var references = sp.GetRequiredService<MdaReferenceArtifactLoader>().Load(schemas);
            return sp.GetRequiredService<MdaContractsBundleConsistencyValidator>().Validate(schemas, references);
        });
        builder.Services.AddSingleton<IMdaContractsStore, MdaContractsStore>();
    }

    public static void UseSprechenContracts(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Frühes Laden lässt den Start bei ungültigen oder fehlenden Modellartefakten scheitern.
        _ = app.Services.GetRequiredService<IMdaContractsStore>();

        app.MapSprechenDefaultEndpoints();

        app.MapGet("/", () => "Contracts service online");

        app.MapGet(ContractsApiRoutes.FormsCatalogTemplate, (HttpContext http, IMdaContractsStore store) =>
        {
            return ReturnArtifact(http, store.FormsCatalog, cacheControl: "no-cache");
        });

        app.MapGet(ContractsApiRoutes.FormContractTemplate, (HttpContext http, IMdaContractsStore store, string applicationId, string formId, string contractVersion) =>
        {
            if (!store.TryGetFormContract(applicationId, formId, contractVersion, out var artifact))
            {
                return Results.NotFound();
            }

            return ReturnArtifact(http, artifact, cacheControl: "public, max-age=31536000, immutable");
        });

        app.MapGet(ContractsApiRoutes.FieldDefinitionsTemplate, (HttpContext http, IMdaContractsStore store, string fieldDefinitionsId, string catalogVersion) =>
        {
            if (!store.TryGetFieldDefinitions(fieldDefinitionsId, catalogVersion, out var artifact))
            {
                return Results.NotFound();
            }

            return ReturnArtifact(http, artifact, cacheControl: "public, max-age=31536000, immutable");
        });

        app.MapGet(ContractsApiRoutes.VariableDefinitionsTemplate, (HttpContext http, IMdaContractsStore store, string variableDefinitionsId, string catalogVersion) =>
        {
            if (!store.TryGetVariableDefinitions(variableDefinitionsId, catalogVersion, out var artifact))
            {
                return Results.NotFound();
            }

            return ReturnArtifact(http, artifact, cacheControl: "public, max-age=31536000, immutable");
        });

        app.MapGet(ContractsApiRoutes.FormVoiceProfileTemplate, (HttpContext http, IMdaContractsStore store, string formVoiceProfileId, string profileVersion) =>
        {
            if (!store.TryGetFormVoiceProfile(formVoiceProfileId, profileVersion, out var artifact))
            {
                return Results.NotFound();
            }

            return ReturnArtifact(http, artifact, cacheControl: "public, max-age=31536000, immutable");
        });

        app.MapGet(ContractsApiRoutes.ApplicationSelectionDialogTemplate, (HttpContext http, IMdaContractsStore store, string selectionDialogId, string modelVersion) =>
        {
            if (!store.TryGetApplicationSelectionDialog(selectionDialogId, modelVersion, out var artifact))
            {
                return Results.NotFound();
            }

            return ReturnArtifact(http, artifact, cacheControl: "public, max-age=31536000, immutable");
        });

        app.MapGet(ContractsApiRoutes.ApplicationSelectionDialogProfileTemplate, (HttpContext http, IMdaContractsStore store, string profileId, string profileVersion) =>
        {
            if (!store.TryGetApplicationSelectionDialogProfile(profileId, profileVersion, out var artifact))
            {
                return Results.NotFound();
            }

            return ReturnArtifact(http, artifact, cacheControl: "public, max-age=31536000, immutable");
        });

        app.MapGet(ContractsApiRoutes.FormsCatalogSchemaTemplate, (HttpContext http, IMdaContractsStore store) =>
        {
            return ReturnArtifact(http, store.FormsCatalogSchema, cacheControl: "public, max-age=31536000, immutable");
        });

        app.MapGet(ContractsApiRoutes.LocalizationPrimitivesSchemaTemplate, (HttpContext http, IMdaContractsStore store) =>
        {
            return ReturnArtifact(http, store.LocalizationPrimitivesSchema, cacheControl: "public, max-age=31536000, immutable");
        });

        app.MapGet(ContractsApiRoutes.ExpressionLanguageSchemaTemplate, (HttpContext http, IMdaContractsStore store) =>
        {
            return ReturnArtifact(http, store.ExpressionLanguageSchema, cacheControl: "public, max-age=31536000, immutable");
        });

        app.MapGet(ContractsApiRoutes.ConstraintLanguageSchemaTemplate, (HttpContext http, IMdaContractsStore store) =>
        {
            return ReturnArtifact(http, store.ConstraintLanguageSchema, cacheControl: "public, max-age=31536000, immutable");
        });

        app.MapGet(ContractsApiRoutes.PrivacyPolicySchemaTemplate, (HttpContext http, IMdaContractsStore store) =>
        {
            return ReturnArtifact(http, store.PrivacyPolicySchema, cacheControl: "public, max-age=31536000, immutable");
        });

        app.MapGet(ContractsApiRoutes.FieldDefinitionsSchemaTemplate, (HttpContext http, IMdaContractsStore store) =>
        {
            return ReturnArtifact(http, store.FieldDefinitionsSchema, cacheControl: "public, max-age=31536000, immutable");
        });

        app.MapGet(ContractsApiRoutes.VariableDefinitionsSchemaTemplate, (HttpContext http, IMdaContractsStore store) =>
        {
            return ReturnArtifact(http, store.VariableDefinitionsSchema, cacheControl: "public, max-age=31536000, immutable");
        });

        app.MapGet(ContractsApiRoutes.FormVoiceProfileSchemaTemplate, (HttpContext http, IMdaContractsStore store) =>
        {
            return ReturnArtifact(http, store.FormVoiceProfileSchema, cacheControl: "public, max-age=31536000, immutable");
        });

        app.MapGet(ContractsApiRoutes.ApplicationSelectionDialogSchemaTemplate, (HttpContext http, IMdaContractsStore store) =>
        {
            return ReturnArtifact(http, store.ApplicationSelectionDialogSchema, cacheControl: "public, max-age=31536000, immutable");
        });

        app.MapGet(ContractsApiRoutes.ApplicationSelectionDialogProfileSchemaTemplate, (HttpContext http, IMdaContractsStore store) =>
        {
            return ReturnArtifact(http, store.ApplicationSelectionDialogProfileSchema, cacheControl: "public, max-age=31536000, immutable");
        });

        app.MapGet(ContractsApiRoutes.FormContractSchemaTemplate, (HttpContext http, IMdaContractsStore store) =>
        {
            return ReturnArtifact(http, store.FormContractSchema, cacheControl: "public, max-age=31536000, immutable");
        });
    }

    private static IResult ReturnArtifact(HttpContext http, MdaJsonArtifact artifact, string cacheControl)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheControl);

        http.Response.Headers.ETag = artifact.ETag;
        http.Response.Headers.CacheControl = cacheControl;

        if (MatchesIfNoneMatch(http, artifact.ETag))
        {
            return Results.StatusCode(StatusCodes.Status304NotModified);
        }

        return Results.Bytes(artifact.Content, artifact.ContentType);
    }

    private static bool MatchesIfNoneMatch(HttpContext http, string etag)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentException.ThrowIfNullOrWhiteSpace(etag);

        var ifNoneMatch = http.Request.GetTypedHeaders().IfNoneMatch;
        if (ifNoneMatch is null || ifNoneMatch.Count == 0)
        {
            return false;
        }

        return ifNoneMatch.Any(value => string.Equals(value.ToString(), etag, StringComparison.Ordinal));
    }
}
