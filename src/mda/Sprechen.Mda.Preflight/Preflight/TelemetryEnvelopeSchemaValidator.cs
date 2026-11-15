using System.Text;
using System.Text.Json;
using Json.Schema;
using Sprechen.Mda.Contracts.V1.RuntimeArtifacts;
using Sprechen.Mda.SchemaBundle;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Telemetrieumschläge und gespeicherte Datensätze gegen das Ereignisschema.
/// Die zum Ereignisnamen gehörende Definition begrenzt Fehlermeldungen auf die betroffenen Regeln.
/// Syntaxfehler, doppelte Mitglieder und Schemaverstöße werden getrennt ausgewiesen.
/// </summary>
public sealed class TelemetryEnvelopeSchemaValidator
{
    private const string FragmentsPath = "evaluationsmodell/schemas/telemetry-fragments.schema.json";
    private const string EventsPath = "evaluationsmodell/schemas/telemetry-events.schema.json";

    private const string DefinitionPointerPrefix = "#/$defs/";
    private const string EventMemberName = "event";

    /// <summary>
    /// Fehlerkennung für fehlendes, syntaktisch ungültiges oder nicht objektförmiges JSON.
    /// </summary>
    private const string JsonKeyword = "json";

    /// <summary>
    /// Fehlerkennung für doppelte JSON-Mitglieder. Sie werden vor der Schemaprüfung erkannt,
    /// damit mehrfach angegebene Werte nicht unbemerkt verworfen werden.
    /// </summary>
    private const string DuplicateMemberKeyword = "duplicateMember";

    /// <summary>
    /// Schemaschlüsselwort für den festgelegten Ereignisnamen.
    /// </summary>
    private const string ConstKeyword = "const";

    private const string RootInstanceLocation = "$";
    private const string EventInstanceLocation = "/" + EventMemberName;

    /// <summary>
    /// Aktiviert die Prüfung von <c>format</c>, damit etwa Zeitstempel
    /// nicht nur auf ihren Zeichenkettentyp geprüft werden.
    /// </summary>
    private static readonly EvaluationOptions EvaluationOptions = new()
    {
        OutputFormat = OutputFormat.Hierarchical,
        RequireFormatValidation = true,
    };

    private static readonly Lazy<IReadOnlyDictionary<string, JsonSchema>> EventSchemasByEventName =
        new(() => BuildEventSchemasByEventName(withRecordReceivedAt: false), LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly Lazy<IReadOnlyDictionary<string, JsonSchema>> RecordSchemasByEventName =
        new(() => BuildEventSchemasByEventName(withRecordReceivedAt: true), LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly JsonSchemaRefusalCollector _refusalCollector = new();

    /// <summary>
    /// Ereignisnamen der geladenen Schemadefinitionen.
    /// </summary>
    public static IReadOnlyCollection<string> DefinedEventNames => EventSchemasByEventName.Value.Keys.ToArray();

    /// <summary>
    /// Liefert Befunde für einen serialisierten Ereignisumschlag. Eine leere Liste bedeutet,
    /// dass die durchgeführten Syntax-, Duplikat- und Schemaprüfungen erfolgreich waren.
    /// </summary>
    public IReadOnlyList<JsonSchemaRefusal> CollectRefusals(string serializedEnvelope)
    {
        return CollectRefusals(serializedEnvelope, EventSchemasByEventName.Value);
    }

    /// <summary>
    /// Liefert Befunde für einen gespeicherten Datensatz einschließlich
    /// des vorgeschriebenen serverseitigen Empfangszeitpunkts.
    /// </summary>
    public IReadOnlyList<JsonSchemaRefusal> CollectRecordRefusals(string serializedLine)
    {
        return CollectRefusals(serializedLine, RecordSchemasByEventName.Value);
    }

    private IReadOnlyList<JsonSchemaRefusal> CollectRefusals(
        string serializedEnvelope,
        IReadOnlyDictionary<string, JsonSchema> schemasByEventName)
    {
        if (string.IsNullOrWhiteSpace(serializedEnvelope))
        {
            return [new JsonSchemaRefusal(
                RootInstanceLocation,
                JsonKeyword,
                "The evaluation telemetry envelope carries no text.")];
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(serializedEnvelope);
        }
        catch (JsonException exception)
        {
            return [new JsonSchemaRefusal(RootInstanceLocation, JsonKeyword, exception.Message)];
        }

        using (document)
        {
            return CollectRefusals(document.RootElement, schemasByEventName);
        }
    }

    /// <summary>
    /// Liefert Befunde für einen bereits geparsten Ereignisumschlag.
    /// </summary>
    public IReadOnlyList<JsonSchemaRefusal> CollectRefusals(JsonElement envelope)
    {
        return CollectRefusals(envelope, EventSchemasByEventName.Value);
    }

    private IReadOnlyList<JsonSchemaRefusal> CollectRefusals(
        JsonElement envelope,
        IReadOnlyDictionary<string, JsonSchema> schemasByEventName)
    {
        if (envelope.ValueKind != JsonValueKind.Object)
        {
            return [new JsonSchemaRefusal(
                RootInstanceLocation,
                JsonKeyword,
                $"The evaluation telemetry envelope is a JSON {envelope.ValueKind} and not an object.")];
        }

        if (FindDuplicateObjectMember(envelope, string.Empty) is { } duplicateMember)
        {
            return [duplicateMember];
        }

        if (!TryReadEventName(envelope, out var eventName))
        {
            return [new JsonSchemaRefusal(
                EventInstanceLocation,
                ConstKeyword,
                "The evaluation telemetry envelope does not name its event as a string.")];
        }

        if (!schemasByEventName.TryGetValue(eventName, out var eventSchema))
        {
            return [new JsonSchemaRefusal(
                EventInstanceLocation,
                ConstKeyword,
                $"The model defines no evaluation telemetry event named '{eventName}'.")];
        }

        return _refusalCollector.Collect(eventSchema.Evaluate(envelope, EvaluationOptions));
    }

    /// <summary>
    /// Formatiert die Befunde eines Ereignisumschlags mit Ort und Regel.
    /// Bei erfolgreicher Prüfung ist das Ergebnis leer.
    /// </summary>
    public string DescribeRefusal(string serializedEnvelope)
    {
        return _refusalCollector.Describe(CollectRefusals(serializedEnvelope));
    }

    /// <summary>
    /// Formatiert die Befunde eines gespeicherten Datensatzes.
    /// Bei erfolgreicher Prüfung ist das Ergebnis leer.
    /// </summary>
    public string DescribeRecordRefusal(string serializedLine)
    {
        return _refusalCollector.Describe(CollectRecordRefusals(serializedLine));
    }

    /// <summary>
    /// Wirft bei einem ungültigen gespeicherten Datensatz eine <see cref="JsonException"/>
    /// mit den Fundstellen und verletzten Regeln.
    /// </summary>
    public void ValidateRecord(string serializedLine)
    {
        var refusals = CollectRecordRefusals(serializedLine);
        if (refusals.Count > 0)
        {
            throw new JsonException(
                $"The evaluation telemetry record line is not canonical: {_refusalCollector.Describe(refusals)}");
        }
    }

    /// <summary>
    /// Wirft bei einem ungültigen Ereignisumschlag eine <see cref="JsonException"/>
    /// mit den Fundstellen und verletzten Regeln.
    /// </summary>
    public void Validate(string serializedEnvelope)
    {
        var refusals = CollectRefusals(serializedEnvelope);
        if (refusals.Count > 0)
        {
            throw new JsonException(
                $"The evaluation telemetry envelope is not canonical: {_refusalCollector.Describe(refusals)}");
        }
    }

    private static bool TryReadEventName(JsonElement envelope, out string eventName)
    {
        if (envelope.TryGetProperty(EventMemberName, out var member)
            && member.ValueKind == JsonValueKind.String
            && member.GetString() is { } value)
        {
            eventName = value;
            return true;
        }

        eventName = string.Empty;
        return false;
    }

    /// <summary>
    /// Erzeugt je Ereignis ein Schema aus der Ereignisdefinition und den zusätzlichen Regeln
    /// für gesendete oder gespeicherte Daten. Beide Teile werden aus dem Modell gelesen,
    /// damit diese Prüfung keine abweichenden Regeln nachbildet.
    /// </summary>
    private static IReadOnlyDictionary<string, JsonSchema> BuildEventSchemasByEventName(bool withRecordReceivedAt)
    {
        var registry = new SchemaRegistry();
        _ = JsonSchema.FromText(
            Encoding.UTF8.GetString(MdaEmbeddedRuntimeArtifacts.ReadAllBytes(FragmentsPath)),
            new BuildOptions { SchemaRegistry = registry });

        var eventsText = Encoding.UTF8.GetString(MdaEmbeddedRuntimeArtifacts.ReadAllBytes(EventsPath));
        _ = JsonSchema.FromText(eventsText, new BuildOptions { SchemaRegistry = registry });

        using var eventsDocument = JsonDocument.Parse(eventsText);
        var eventsSchemaId = eventsDocument.RootElement.GetProperty("$id").GetString();
        var definitions = eventsDocument.RootElement.GetProperty("$defs");
        var eventSchemasByEventName = new Dictionary<string, JsonSchema>(StringComparer.Ordinal);
        // Die Schemawurzel verweist auf die gesendete Form. Deren erster allOf-Teil
        // verweist auf die gemeinsame Variantenliste mit oneOf.
        var sentFormName = ReadDefinitionName(eventsDocument.RootElement);
        var variantsDefinitionName = ReadDefinitionName(
            definitions.GetProperty(sentFormName).GetProperty("allOf")[0]);
        var formName = withRecordReceivedAt
            ? ReadStoredFormName(definitions, variantsDefinitionName, sentFormName)
            : sentFormName;
        var disposition = ReadDisposition(definitions.GetProperty(formName), formName);
        foreach (var choice in definitions.GetProperty(variantsDefinitionName).GetProperty("oneOf").EnumerateArray())
        {
            var definitionName = ReadDefinitionName(choice);
            var eventName = ReadPinnedEventName(definitions.GetProperty(definitionName), definitionName);
            var eventSchema = JsonSchema.FromText(
                $$$"""{"allOf": [{"$ref": "{{{eventsSchemaId}}}{{{DefinitionPointerPrefix}}}{{{definitionName}}}"}, {{{disposition}}}]}""",
                new BuildOptions { SchemaRegistry = registry });
            if (!eventSchemasByEventName.TryAdd(eventName, eventSchema))
            {
                // Mehrere Definitionen desselben Ereignisnamens würden die Schemaauswahl mehrdeutig machen.
                throw new InvalidOperationException(
                    $"The evaluation telemetry event '{eventName}' is defined more than once.");
            }
        }

        return eventSchemasByEventName;
    }

    /// <summary>
    /// Ermittelt neben der gesendeten Form genau eine gespeicherte Form,
    /// die dieselbe Variantenliste referenziert.
    /// </summary>
    private static string ReadStoredFormName(
        JsonElement definitions,
        string variantsDefinitionName,
        string sentFormName)
    {
        var storedFormNames = definitions.EnumerateObject()
            .Where(definition => definition.Value.TryGetProperty("allOf", out var parts)
                && parts.GetArrayLength() > 0
                && parts[0].TryGetProperty("$ref", out var reference)
                && string.Equals(
                    reference.GetString(),
                    DefinitionPointerPrefix + variantsDefinitionName,
                    StringComparison.Ordinal))
            .Select(static definition => definition.Name)
            .Where(name => !string.Equals(name, sentFormName, StringComparison.Ordinal))
            .ToArray();

        if (storedFormNames.Length != 1)
        {
            throw new InvalidOperationException(
                $"Exactly one stored shape must build on '{variantsDefinitionName}' beside '{sentFormName}'.");
        }

        return storedFormNames[0];
    }

    /// <summary>
    /// Liest den zweiten <c>allOf</c>-Teil mit den zusätzlichen Regeln
    /// zum serverseitigen Empfangszeitpunkt.
    /// </summary>
    private static string ReadDisposition(JsonElement form, string formName)
    {
        var parts = form.GetProperty("allOf");
        if (parts.GetArrayLength() != 2)
        {
            // Zusätzliche allOf-Teile würden sonst bei der ereignisspezifischen Prüfung übergangen.
            throw new InvalidOperationException(
                $"The evaluation telemetry shape '{formName}' must state the variants and exactly one disposition.");
        }

        return parts[1].GetRawText();
    }

    private static string ReadDefinitionName(JsonElement choice)
    {
        var pointer = choice.TryGetProperty("$ref", out var reference) ? reference.GetString() : null;
        if (pointer is null || !pointer.StartsWith(DefinitionPointerPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Every evaluation telemetry event choice must reference one definition of this schema.");
        }

        return pointer[DefinitionPointerPrefix.Length..];
    }

    private static string ReadPinnedEventName(JsonElement definition, string definitionName)
    {
        var pinnedEventNames = definition.GetProperty("allOf").EnumerateArray()
            .Where(static part => part.TryGetProperty("properties", out _))
            .Select(static part => part.GetProperty("properties"))
            .Where(static properties => properties.TryGetProperty(EventMemberName, out _))
            .Select(static properties => properties.GetProperty(EventMemberName))
            .Where(static eventProperty => eventProperty.TryGetProperty(ConstKeyword, out _))
            .Select(static eventProperty => eventProperty.GetProperty(ConstKeyword).GetString())
            .OfType<string>()
            .ToArray();

        if (pinnedEventNames.Length != 1)
        {
            throw new InvalidOperationException(
                $"The evaluation telemetry event definition '{definitionName}' must pin exactly one event value.");
        }

        return pinnedEventNames[0];
    }

    private static JsonSchemaRefusal? FindDuplicateObjectMember(JsonElement element, string instanceLocation)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in element.EnumerateObject())
                {
                    var memberLocation = $"{instanceLocation}/{EscapePointerSegment(property.Name)}";
                    if (!names.Add(property.Name))
                    {
                        return new JsonSchemaRefusal(
                            memberLocation,
                            DuplicateMemberKeyword,
                            "The evaluation telemetry envelope states this member more than once.");
                    }

                    if (FindDuplicateObjectMember(property.Value, memberLocation) is { } nested)
                    {
                        return nested;
                    }
                }

                return null;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    if (FindDuplicateObjectMember(item, $"{instanceLocation}/{index}") is { } nested)
                    {
                        return nested;
                    }

                    index++;
                }

                return null;
            default:
                return null;
        }
    }

    private static string EscapePointerSegment(string memberName)
    {
        return memberName
            .Replace("~", "~0", StringComparison.Ordinal)
            .Replace("/", "~1", StringComparison.Ordinal);
    }
}
