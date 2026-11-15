using System.Text.Json;

namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Kopiert lokalisierte Texte, Metadaten, Datenschutzregeln und JSON-Werte
/// für die Auflösung von Formularverträgen. JSON-Werte bleiben nach dem
/// Freigeben ihres ursprünglichen Dokuments verfügbar.
/// </summary>
internal static class FormContractResolutionJsonCloner
{
    internal static SensitiveDataPolicy? CloneSensitiveDataPolicy(SensitiveDataPolicy? policy)
    {
        if (policy is null)
        {
            return null;
        }

        return new SensitiveDataPolicy
        {
            VoiceInputPolicy = policy.VoiceInputPolicy,
            ReadbackPolicy = policy.ReadbackPolicy,
            SilentInputFallback = policy.SilentInputFallback
        };
    }

    internal static TraceInfo? CloneTraceInfo(TraceInfo? trace)
    {
        if (trace is null)
        {
            return null;
        }

        return new TraceInfo
        {
            RequestIds = trace.RequestIds is null ? null : [.. trace.RequestIds],
            Sources = trace.Sources is null ? null : [.. trace.Sources],
            Rationale = trace.Rationale
        };
    }

    internal static LocalizedText CloneRequiredLocalizedText(LocalizedText text)
    {
        var clone = new LocalizedText();
        foreach (var pair in text)
        {
            clone[pair.Key] = pair.Value;
        }

        return clone;
    }

    internal static LocalizedText? CloneOptionalLocalizedText(LocalizedText? text)
    {
        return text is null ? null : CloneRequiredLocalizedText(text);
    }

    internal static List<LocalizedText>? CloneLocalizedTextList(List<LocalizedText>? texts)
    {
        if (texts is null)
        {
            return null;
        }

        var clones = new List<LocalizedText>(texts.Count);
        foreach (var text in texts)
        {
            clones.Add(CloneRequiredLocalizedText(text));
        }

        return clones;
    }

    internal static Dictionary<string, JsonElement>? CloneExtensions(Dictionary<string, JsonElement>? source)
    {
        if (source is null)
        {
            return null;
        }

        var clone = new Dictionary<string, JsonElement>(source.Count, StringComparer.Ordinal);
        foreach (var pair in source)
        {
            clone[pair.Key] = CloneJsonElement(pair.Value);
        }

        return clone;
    }

    internal static JsonElement? CloneJsonElement(JsonElement? element)
    {
        return element is null ? null : CloneJsonElement(element.Value);
    }

    internal static JsonElement CloneJsonElement(JsonElement element)
    {
        using var document = JsonDocument.Parse(element.GetRawText());
        return document.RootElement.Clone();
    }

    internal static JsonElement CloneConstraintJsonElement(ConstraintDefinition constraint)
    {
        var json = JsonSerializer.Serialize(constraint);
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
