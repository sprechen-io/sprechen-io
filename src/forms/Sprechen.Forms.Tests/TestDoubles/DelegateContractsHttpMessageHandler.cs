using System.Net;

namespace Sprechen.Forms.Tests.TestDoubles;

internal sealed class DelegateContractsHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<CancellationToken, Task<Stream>> _getCatalog;
    private readonly Func<string, string, string, CancellationToken, Task<Stream>> _getForm;
    private readonly Func<string, string, CancellationToken, Task<Stream>> _getFieldDefinitions;
    private readonly Func<string, string, CancellationToken, Task<Stream>> _getVariableDefinitions;
    private readonly Func<string, string, CancellationToken, Task<Stream>> _getFormVoiceProfile;
    private readonly Func<string, string, CancellationToken, Task<Stream>> _getApplicationSelectionDialog;
    private readonly Func<string, string, CancellationToken, Task<Stream>> _getApplicationSelectionDialogProfile;
    private readonly Func<CancellationToken, Task<Stream>> _getCatalogSchema;
    private readonly Func<CancellationToken, Task<Stream>> _getFieldDefinitionsSchema;
    private readonly Func<CancellationToken, Task<Stream>> _getVariableDefinitionsSchema;
    private readonly Func<CancellationToken, Task<Stream>> _getFormVoiceProfileSchema;
    private readonly Func<CancellationToken, Task<Stream>> _getApplicationSelectionDialogSchema;
    private readonly Func<CancellationToken, Task<Stream>> _getApplicationSelectionDialogProfileSchema;
    private readonly Func<CancellationToken, Task<Stream>> _getFormSchema;

    public DelegateContractsHttpMessageHandler(
        Func<CancellationToken, Task<Stream>> getCatalog,
        Func<string, string, string, CancellationToken, Task<Stream>> getForm,
        Func<string, string, CancellationToken, Task<Stream>> getFieldDefinitions,
        Func<CancellationToken, Task<Stream>> getCatalogSchema,
        Func<CancellationToken, Task<Stream>> getFieldDefinitionsSchema,
        Func<CancellationToken, Task<Stream>> getFormSchema)
        : this(
            getCatalog,
            getForm,
            getFieldDefinitions,
            (_, _, _) => throw new InvalidOperationException("Variable catalog should not be requested."),
            (_, _, _) => throw new InvalidOperationException("Form voice profile should not be requested."),
            (_, _, _) => throw new InvalidOperationException("Catalog selection dialog should not be requested."),
            (_, _, _) => throw new InvalidOperationException("Selection dialog profile should not be requested."),
            getCatalogSchema,
            getFieldDefinitionsSchema,
            _ => throw new InvalidOperationException("Variable catalog schema should not be requested."),
            _ => throw new InvalidOperationException("Form voice profile schema should not be requested."),
            _ => throw new InvalidOperationException("Catalog selection dialog schema should not be requested."),
            _ => throw new InvalidOperationException("Selection dialog profile schema should not be requested."),
            getFormSchema)
    {
    }

    public DelegateContractsHttpMessageHandler(
        Func<CancellationToken, Task<Stream>> getCatalog,
        Func<string, string, string, CancellationToken, Task<Stream>> getForm,
        Func<string, string, CancellationToken, Task<Stream>> getFieldDefinitions,
        Func<string, string, CancellationToken, Task<Stream>> getVariableDefinitions,
        Func<string, string, CancellationToken, Task<Stream>> getApplicationSelectionDialog,
        Func<string, string, CancellationToken, Task<Stream>> getApplicationSelectionDialogProfile,
        Func<CancellationToken, Task<Stream>> getCatalogSchema,
        Func<CancellationToken, Task<Stream>> getFieldDefinitionsSchema,
        Func<CancellationToken, Task<Stream>> getVariableDefinitionsSchema,
        Func<CancellationToken, Task<Stream>> getApplicationSelectionDialogSchema,
        Func<CancellationToken, Task<Stream>> getApplicationSelectionDialogProfileSchema,
        Func<CancellationToken, Task<Stream>> getFormSchema)
        : this(
            getCatalog,
            getForm,
            getFieldDefinitions,
            getVariableDefinitions,
            (_, _, _) => throw new InvalidOperationException("Form voice profile should not be requested."),
            getApplicationSelectionDialog,
            getApplicationSelectionDialogProfile,
            getCatalogSchema,
            getFieldDefinitionsSchema,
            getVariableDefinitionsSchema,
            _ => throw new InvalidOperationException("Form voice profile schema should not be requested."),
            getApplicationSelectionDialogSchema,
            getApplicationSelectionDialogProfileSchema,
            getFormSchema)
    {
    }

    public DelegateContractsHttpMessageHandler(
        Func<CancellationToken, Task<Stream>> getCatalog,
        Func<string, string, string, CancellationToken, Task<Stream>> getForm,
        Func<string, string, CancellationToken, Task<Stream>> getFieldDefinitions,
        Func<string, string, CancellationToken, Task<Stream>> getVariableDefinitions,
        Func<string, string, CancellationToken, Task<Stream>> getFormVoiceProfile,
        Func<string, string, CancellationToken, Task<Stream>> getApplicationSelectionDialog,
        Func<string, string, CancellationToken, Task<Stream>> getApplicationSelectionDialogProfile,
        Func<CancellationToken, Task<Stream>> getCatalogSchema,
        Func<CancellationToken, Task<Stream>> getFieldDefinitionsSchema,
        Func<CancellationToken, Task<Stream>> getVariableDefinitionsSchema,
        Func<CancellationToken, Task<Stream>> getFormVoiceProfileSchema,
        Func<CancellationToken, Task<Stream>> getApplicationSelectionDialogSchema,
        Func<CancellationToken, Task<Stream>> getApplicationSelectionDialogProfileSchema,
        Func<CancellationToken, Task<Stream>> getFormSchema)
    {
        _getCatalog = getCatalog ?? throw new ArgumentNullException(nameof(getCatalog));
        _getForm = getForm ?? throw new ArgumentNullException(nameof(getForm));
        _getFieldDefinitions = getFieldDefinitions ?? throw new ArgumentNullException(nameof(getFieldDefinitions));
        _getVariableDefinitions = getVariableDefinitions ?? throw new ArgumentNullException(nameof(getVariableDefinitions));
        _getFormVoiceProfile = getFormVoiceProfile ?? throw new ArgumentNullException(nameof(getFormVoiceProfile));
        _getApplicationSelectionDialog = getApplicationSelectionDialog ?? throw new ArgumentNullException(nameof(getApplicationSelectionDialog));
        _getApplicationSelectionDialogProfile = getApplicationSelectionDialogProfile ?? throw new ArgumentNullException(nameof(getApplicationSelectionDialogProfile));
        _getCatalogSchema = getCatalogSchema ?? throw new ArgumentNullException(nameof(getCatalogSchema));
        _getFieldDefinitionsSchema = getFieldDefinitionsSchema ?? throw new ArgumentNullException(nameof(getFieldDefinitionsSchema));
        _getVariableDefinitionsSchema = getVariableDefinitionsSchema ?? throw new ArgumentNullException(nameof(getVariableDefinitionsSchema));
        _getFormVoiceProfileSchema = getFormVoiceProfileSchema ?? throw new ArgumentNullException(nameof(getFormVoiceProfileSchema));
        _getApplicationSelectionDialogSchema = getApplicationSelectionDialogSchema ?? throw new ArgumentNullException(nameof(getApplicationSelectionDialogSchema));
        _getApplicationSelectionDialogProfileSchema = getApplicationSelectionDialogProfileSchema ?? throw new ArgumentNullException(nameof(getApplicationSelectionDialogProfileSchema));
        _getFormSchema = getFormSchema ?? throw new ArgumentNullException(nameof(getFormSchema));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var stream = await ResolveAsync(request.RequestUri, cancellationToken).ConfigureAwait(false);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(stream),
            RequestMessage = request
        };
    }

    private Task<Stream> ResolveAsync(Uri? requestUri, CancellationToken cancellationToken)
    {
        var path = requestUri?.AbsolutePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("Contracts API test request did not contain a path.");
        }

        var segments = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2 || !string.Equals(segments[0], "contracts", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unsupported contracts API test path '{path}'.");
        }

        if (segments is ["contracts", "forms-catalog"])
        {
            return _getCatalog(cancellationToken);
        }

        if (segments is ["contracts", "field-definitions", var fieldDefinitionsId, var catalogVersion])
        {
            return _getFieldDefinitions(Decode(fieldDefinitionsId), Decode(catalogVersion), cancellationToken);
        }

        if (segments is ["contracts", "variable-definitions", var variableDefinitionsId, var variableCatalogVersion])
        {
            return _getVariableDefinitions(Decode(variableDefinitionsId), Decode(variableCatalogVersion), cancellationToken);
        }

        if (segments is ["contracts", "form-voice-profiles", var formVoiceProfileId, var profileVersion])
        {
            return _getFormVoiceProfile(Decode(formVoiceProfileId), Decode(profileVersion), cancellationToken);
        }

        if (segments is ["contracts", "application-selection-dialogs", var selectionDialogId, var modelVersion])
        {
            return _getApplicationSelectionDialog(Decode(selectionDialogId), Decode(modelVersion), cancellationToken);
        }

        if (segments is ["contracts", "application-selection-dialog-profiles", var profileId, var selectionProfileVersion])
        {
            return _getApplicationSelectionDialogProfile(Decode(profileId), Decode(selectionProfileVersion), cancellationToken);
        }

        if (segments is ["contracts", "schemas", var schemaId])
        {
            return ResolveSchemaAsync(schemaId, cancellationToken);
        }

        if (segments is ["contracts", var applicationId, var formId, var contractVersion])
        {
            return _getForm(Decode(applicationId), Decode(formId), Decode(contractVersion), cancellationToken);
        }

        throw new InvalidOperationException($"Unsupported contracts API test path '{path}'.");
    }

    private Task<Stream> ResolveSchemaAsync(string schemaId, CancellationToken cancellationToken)
    {
        return schemaId switch
        {
            "forms-catalog" => _getCatalogSchema(cancellationToken),
            "field-definitions" => _getFieldDefinitionsSchema(cancellationToken),
            "variable-definitions" => _getVariableDefinitionsSchema(cancellationToken),
            "form-voice-profile" => _getFormVoiceProfileSchema(cancellationToken),
            "application-selection-dialog" => _getApplicationSelectionDialogSchema(cancellationToken),
            "application-selection-dialog-profile" => _getApplicationSelectionDialogProfileSchema(cancellationToken),
            "form-contract" => _getFormSchema(cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported contracts API schema test path '{schemaId}'.")
        };
    }

    private static string Decode(string value)
    {
        return Uri.UnescapeDataString(value);
    }
}
