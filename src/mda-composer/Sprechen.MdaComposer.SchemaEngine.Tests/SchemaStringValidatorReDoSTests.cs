using System.Diagnostics;
using System.Text.Json.Nodes;
using Sprechen.MdaComposer.SchemaEngine;

namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class SchemaStringValidatorReDoSTests
{
    [Fact]
    public void Validate_PathologicalPattern_TimesOutWithError()
    {
        var schema = new SchemaNode
        {
            Name = "$",
            Type = SchemaNodeType.String,
            Pattern = "^(a+)+$",
        };
        var pathologicalInput = new string('a', 32) + "!";
        var document = JsonValue.Create(pathologicalInput);
        var sut = new SchemaValidator();

        var stopwatch = Stopwatch.StartNew();
        var errors = sut.Validate(schema, document);
        stopwatch.Stop();

        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(2),
            $"Validation took {stopwatch.ElapsedMilliseconds}ms — Regex timeout did not trigger; possible ReDoS regression.");
        Assert.Contains(
            errors,
            error => error.Message.Contains("Timeout", StringComparison.OrdinalIgnoreCase)
                || error.Message.Contains("ReDoS", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_BenignPattern_PassesQuickly()
    {
        var schema = new SchemaNode
        {
            Name = "$",
            Type = SchemaNodeType.String,
            Pattern = "^[a-z]+$",
        };
        var document = JsonValue.Create("hello");
        var sut = new SchemaValidator();

        var errors = sut.Validate(schema, document);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_BenignPatternMismatch_ReportsPatternError()
    {
        var schema = new SchemaNode
        {
            Name = "$",
            Type = SchemaNodeType.String,
            Pattern = "^[0-9]+$",
        };
        var document = JsonValue.Create("abc");
        var sut = new SchemaValidator();

        var errors = sut.Validate(schema, document);

        Assert.Single(errors);
        Assert.DoesNotContain(
            errors,
            error => error.Message.Contains("Timeout", StringComparison.OrdinalIgnoreCase));
    }
}
