using System.Text.Json;
using Json.Schema;
using Sprechen.Mda.SchemaBundle;

namespace Sprechen.Mda.Contracts.Tests.Schema;

/// <summary>
/// Prüft aussagekräftige Schemadiagnosen mit Fundstelle, Schlüsselwort und Grund.
/// Fehler nicht zutreffender Alternativen und bloße Folgefehler sollen nicht
/// als zusätzliche Dokumentfehler ausgegeben werden.
/// </summary>
public sealed class JsonSchemaRefusalCollectorTests
{
    private static readonly JsonSchemaRefusalCollector Collector = new();

    [Fact]
    public void Collect_WhenTheDocumentIsRefused_NamesThePlaceTheKeywordAndTheReason()
    {
        var refusals = Collector.Collect(Evaluate(
            """{"type":"object","properties":{"applications":{"type":"array","minItems":1}}}""",
            """{"applications":[]}"""));

        var refusal = Assert.Single(refusals);
        Assert.Equal("/applications", refusal.InstanceLocation);
        Assert.Equal("minItems", refusal.Keyword);
        Assert.NotEmpty(refusal.Message);
    }

    [Fact]
    public void Collect_WhenTheRootItselfIsRefused_NamesTheRootRatherThanAnEmptyLocation()
    {
        var refusals = Collector.Collect(Evaluate(
            """{"type":"object","required":["distributionId"]}""",
            "{}"));

        Assert.Equal("$", Assert.Single(refusals).InstanceLocation);
    }

    [Fact]
    public void Collect_WhenAConditionalTookTheOtherBranch_ReportsNothing()
    {
        // Die if-Bedingung trifft nicht zu. Ihr negatives Teilergebnis ist
        // kein Validierungsfehler des insgesamt gültigen Dokuments.
        var refusals = Collector.Collect(Evaluate(
            """
            {
              "type": "object",
              "if": { "properties": { "kind": { "const": "boundary" } }, "required": ["kind"] },
              "then": { "required": ["bound"] }
            }
            """,
            """{"kind":"plain"}"""));

        Assert.Empty(refusals);
    }

    [Fact]
    public void Collect_WhenASubschemaIsJustFalse_NamesTheKeywordThatAppliedItInsteadOfNothing()
    {
        var refusals = Collector.Collect(Evaluate(
            """{"type":"object","properties":{"a":{}},"unevaluatedProperties":false}""",
            """{"a":1,"b":2}"""));

        var refusal = Assert.Single(refusals);
        Assert.Equal("/b", refusal.InstanceLocation);
        Assert.Equal("unevaluatedProperties", refusal.Keyword);
    }

    [Fact]
    public void Collect_WhenAnotherRuleRefusedTheDocument_DoesNotAlsoNameTheMembersLeftUnevaluated()
    {
        // Das fehlgeschlagene allOf liefert keine ausgewerteten Eigenschaften.
        // Dadurch würde unevaluatedProperties auch das korrekt angegebene a melden.
        var refusals = Collector.Collect(Evaluate(
            LeftoverSchemaText,
            """{"a":1}"""));

        Assert.Equal("required", Assert.Single(refusals).Keyword);
    }

    [Fact]
    public void Collect_WhenOnlyTheLeftoverRuleRefusesTheDocument_StillNamesIt()
    {
        var refusals = Collector.Collect(Evaluate(
            LeftoverSchemaText,
            """{"a":1,"b":2,"c":3}"""));

        var refusal = Assert.Single(refusals);
        Assert.Equal("/c", refusal.InstanceLocation);
        Assert.Equal("unevaluatedProperties", refusal.Keyword);
    }

    [Fact]
    public void Collect_WhenAChoiceSucceededAndASiblingRuleFailed_DoesNotNameTheAlternativesTheDocumentDidNotTake()
    {
        // Die nicht passenden oneOf-Alternativen bleiben im Ergebnisbaum sichtbar,
        // wenn eine andere Regel fehlschlägt. Da oneOf selbst erfolgreich ist,
        // sind diese Alternativen keine zusätzlichen Dokumentfehler.
        var refusals = Collector.Collect(Evaluate(
            """
            {
              "type": "object",
              "additionalProperties": false,
              "properties": { "kind": {} },
              "oneOf": [
                { "properties": { "kind": { "const": "a" } } },
                { "properties": { "kind": { "const": "b" } } },
                { "properties": { "kind": { "const": "c" } } }
              ]
            }
            """,
            """{"kind":"a","stray":1}"""));

        var refusal = Assert.Single(refusals);
        Assert.Equal("/stray", refusal.InstanceLocation);
        Assert.Equal("additionalProperties", refusal.Keyword);
    }

    [Fact]
    public void Collect_WhenNoAlternativeOfAChoiceFits_NamesWhatEachOneRefused()
    {
        var refusals = Collector.Collect(Evaluate(
            """
            {
              "type": "object",
              "oneOf": [
                { "properties": { "kind": { "const": "a" } }, "required": ["kind"] },
                { "properties": { "kind": { "const": "b" } }, "required": ["kind"] }
              ]
            }
            """,
            """{"kind":"z"}"""));

        Assert.Equal(2, refusals.Count(static refusal => refusal.InstanceLocation == "/kind"));
    }

    [Fact]
    public void Describe_JoinsEveryRefusalIntoOneLine()
    {
        var described = Collector.Describe(Evaluate(
            """{"type":"object","required":["distributionId"],"additionalProperties":false}""",
            """{"note":"x"}"""));

        Assert.Contains("$ required:", described, StringComparison.Ordinal);
        Assert.Contains("/note", described, StringComparison.Ordinal);
        Assert.Contains("; ", described, StringComparison.Ordinal);
    }

    private const string LeftoverSchemaText =
        """
        {
          "type": "object",
          "allOf": [ { "$ref": "#/$defs/base" } ],
          "unevaluatedProperties": false,
          "$defs": {
            "base": {
              "properties": { "a": { "type": "integer" }, "b": { "type": "integer" } },
              "required": [ "a", "b" ]
            }
          }
        }
        """;

    private static EvaluationResults Evaluate(string schemaText, string documentJson)
    {
        using var document = JsonDocument.Parse(documentJson);
        return JsonSchema.FromText(schemaText)
            .Evaluate(document.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });
    }
}
