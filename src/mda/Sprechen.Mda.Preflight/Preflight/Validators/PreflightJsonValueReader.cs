using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Gemeinsame Lesehilfen für Zeichenketten, Ganzzahlen und sprachabhängige
/// Texte aus JSON-Werten der Modellvorprüfung.
/// </summary>
internal static class PreflightJsonValueReader
{
    internal static string? TryGetKind(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return TryGetString(element, "kind");
    }

    internal static int? TryGetInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop) || prop.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return prop.GetInt32();
    }

    internal static string? TryGetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop) || prop.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return prop.GetString();
    }

    internal static string? TryGetConstraintStringProperty(ConstraintDefinition constraint, string propertyName)
    {
        if (constraint.AdditionalProperties is null)
        {
            return null;
        }

        if (!constraint.AdditionalProperties.TryGetValue(propertyName, out var prop) || prop.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return prop.GetString();
    }

    internal static bool TryGetLocalizedTextValue(JsonElement container, string propertyName, string language, out string? value)
    {
        value = null;

        if (container.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!container.TryGetProperty(propertyName, out var localizedText) || localizedText.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!localizedText.TryGetProperty(language, out var localized) || localized.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = localized.GetString();
        return true;
    }
}
