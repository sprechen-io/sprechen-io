using System.Text.Json;
using System.Text.Json.Nodes;

namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Fügt geprüfte Anwendungsdistributionen dem Katalog hinzu und weist kollidierende Anwendungskennungen zurück. Die Reihenfolge des Aufrufers bleibt für stabile Inhalte und ETags erhalten. Der Standard-JSON-Encoder hält die Ausgabe ASCII-kompatibel.
/// </summary>
internal sealed class FormsCatalogComposer
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true
    };

    public byte[] Compose(byte[] neutralCatalogBytes, IReadOnlyList<ApplicationDistributionFragment> distributions)
    {
        ArgumentNullException.ThrowIfNull(neutralCatalogBytes);
        ArgumentNullException.ThrowIfNull(distributions);

        var neutral = ParseObject(neutralCatalogBytes, "neutral forms catalog");
        var neutralApplications = GetApplicationsArray(neutral, "neutral forms catalog");

        var existingIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var application in neutralApplications)
        {
            existingIds.Add(GetApplicationId(application, "neutral forms catalog"));
        }

        foreach (var distribution in distributions)
        {
            var context = $"distribution '{distribution.DistributionId}'";
            foreach (var application in distribution.Applications)
            {
                var applicationId = GetApplicationId(application, context);
                if (!existingIds.Add(applicationId))
                {
                    throw new InvalidOperationException(
                        $"Forms-catalog composition produced a duplicate applicationId '{applicationId}' ({context}).");
                }

                neutralApplications.Add(application!.DeepClone());
            }
        }

        return JsonSerializer.SerializeToUtf8Bytes(neutral, WriteOptions);
    }

    private static JsonObject ParseObject(byte[] bytes, string context)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(bytes);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid JSON ({context}).", ex);
        }

        return node as JsonObject
            ?? throw new InvalidOperationException($"Expected a JSON object ({context}).");
    }

    private static JsonArray GetApplicationsArray(JsonObject root, string context)
    {
        if (root["applications"] is not JsonArray applications)
        {
            throw new InvalidOperationException($"Missing or non-array 'applications' ({context}).");
        }

        return applications;
    }

    private static string GetApplicationId(JsonNode? application, string context)
    {
        if (application is not JsonObject applicationObject
            || applicationObject["applicationId"] is not JsonValue idValue
            || idValue.GetValueKind() != JsonValueKind.String)
        {
            throw new InvalidOperationException($"Application descriptor is missing a string 'applicationId' ({context}).");
        }

        return idValue.GetValue<string>();
    }
}
