using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Sprechen.Mda.Contracts.V1.RuntimeArtifacts;

namespace Sprechen.Contracts.Tests.ApiClient;

internal sealed class EmbeddedContractsHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, int> _callCounts = new(StringComparer.Ordinal);
    private string _selectionDialogModelVersion;

    public EmbeddedContractsHttpMessageHandler(string selectionDialogModelVersion = "5")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectionDialogModelVersion);
        _selectionDialogModelVersion = selectionDialogModelVersion.Trim();
    }

    public void SetSelectionDialogModelVersion(string modelVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelVersion);
        _selectionDialogModelVersion = modelVersion.Trim();
    }

    public int GetCallCount(string key)
    {
        return _callCounts.TryGetValue(key, out var count) ? count : 0;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var path = request.RequestUri?.AbsolutePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("Contracts API test request did not contain a path.");
        }

        var (callKey, artifactPath) = ResolveArtifact(path);
        _callCounts[callKey] = GetCallCount(callKey) + 1;

        var content = ResolveContent(callKey, artifactPath);

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(content),
            RequestMessage = request
        });
    }

    private (string CallKey, string ArtifactPath) ResolveArtifact(string path)
    {
        var segments = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2 || !string.Equals(segments[0], "contracts", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unsupported contracts API test path '{path}'.");
        }

        if (segments is ["contracts", "forms-catalog"])
        {
            return (
                "forms-catalog",
                "navigationsmodell/reference-contracts/forms-catalog/forms-catalog.reference.json");
        }

        if (segments is ["contracts", "field-definitions", var fieldDefinitionsId, var catalogVersion])
        {
            var key = $"{Decode(fieldDefinitionsId)}/{Decode(catalogVersion)}";
            return ($"field-definitions:{key}", GetFieldDefinitionsPath(key));
        }

        if (segments is ["contracts", "variable-definitions", var variableDefinitionsId, var variableCatalogVersion])
        {
            var key = $"{Decode(variableDefinitionsId)}/{Decode(variableCatalogVersion)}";
            return ($"variable-definitions:{key}", GetVariableDefinitionsPath(key));
        }

        if (segments is ["contracts", "form-voice-profiles", var formVoiceProfileId, var profileVersion])
        {
            // Dieser Testhandler stellt keine formularspezifischen Dialogprofile bereit.
            var key = $"{Decode(formVoiceProfileId)}/{Decode(profileVersion)}";
            throw new InvalidOperationException($"No form voice profile is available for key '{key}'.");
        }

        if (segments is ["contracts", "application-selection-dialogs", var selectionDialogId, var modelVersion])
        {
            var key = $"{Decode(selectionDialogId)}/{Decode(modelVersion)}";
            return (
                $"application-selection-dialog:{key}",
                GetApplicationSelectionDialogPath(key));
        }

        if (segments is ["contracts", "application-selection-dialog-profiles", var profileId, var selectionProfileVersion])
        {
            var key = $"{Decode(profileId)}/{Decode(selectionProfileVersion)}";
            return (
                $"application-selection-dialog-profile:{key}",
                GetApplicationSelectionDialogProfilePath(key));
        }

        if (segments is ["contracts", var applicationId, var formId, var contractVersion])
        {
            var key = $"{Decode(applicationId)}/{Decode(formId)}/{Decode(contractVersion)}";
            return ($"form-contract:{key}", GetFormContractPath(key));
        }

        throw new InvalidOperationException($"Unsupported contracts API test path '{path}'.");
    }

    private static string Decode(string value)
    {
        return Uri.UnescapeDataString(value);
    }

    private static string GetFormContractPath(string key)
    {
        return key switch
        {
            "sample.simple/two-groups/1" => "apps/sample-simple/form-contracts/form-contract.simple-two-groups.reference.json",
            _ => throw new InvalidOperationException($"Unknown form contract key '{key}'.")
        };
    }

    private string GetApplicationSelectionDialogPath(string key)
    {
        return string.Equals(
                key,
                "sample.application-selection-dialog/5",
                StringComparison.Ordinal)
            ? "navigationsmodell/reference-contracts/application-selection-dialogs/application-selection-dialog.default.reference.json"
            : throw new InvalidOperationException($"Unknown application selection dialog key '{key}'.");
    }

    private byte[] ResolveContent(string callKey, string artifactPath)
    {
        if (string.Equals(callKey, "forms-catalog", StringComparison.Ordinal))
        {
            var content = EmbeddedFormsCatalog.ComposedBytes();
            return string.Equals(_selectionDialogModelVersion, "5", StringComparison.Ordinal)
                ? content
                : RewriteCatalogModelVersion(content);
        }

        return MdaEmbeddedRuntimeArtifacts.ReadAllBytes(artifactPath);
    }

    private byte[] RewriteCatalogModelVersion(byte[] content)
    {
        var root = JsonNode.Parse(content) as JsonObject
            ?? throw new InvalidOperationException("Embedded contract test artifact is not a JSON object.");
        var owner = root["selectionDialog"] as JsonObject
            ?? throw new InvalidOperationException("Embedded forms catalog has no selectionDialog object.");
        owner["modelVersion"] = _selectionDialogModelVersion;
        return JsonSerializer.SerializeToUtf8Bytes(root);
    }

    private static string GetApplicationSelectionDialogProfilePath(string key)
    {
        return key switch
        {
            "sample.application-selection-dialog.default/1" => "navigationsmodell/reference-contracts/application-selection-dialog-profiles/application-selection-dialog-profile.default.reference.json",
            _ => throw new InvalidOperationException($"Unknown application selection dialog profile key '{key}'.")
        };
    }

    private static string GetFieldDefinitionsPath(string key)
    {
        return key switch
        {
            "sample.simple.fields.multilingual/1" => "apps/sample-simple/field-definitions/field-definitions.simple-multilingual.reference.json",
            _ => throw new InvalidOperationException($"Unknown field definitions key '{key}'.")
        };
    }

    private static string GetVariableDefinitionsPath(string key)
    {
        return key switch
        {
            "sample.simple.variables.multilingual/1" => "apps/sample-simple/variable-definitions/variable-definitions.simple-multilingual.reference.json",
            _ => throw new InvalidOperationException($"Unknown variable definitions key '{key}'.")
        };
    }
}
