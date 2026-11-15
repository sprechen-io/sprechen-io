using System.Text.Json.Nodes;

namespace Sprechen.MdaComposer.SchemaEngine.Tests;

public sealed class SchemaValidatorTests
{
    private readonly SchemaValidator _validator = new();

    [Fact]
    public void Valid_string_property_passes()
    {
        var schema = new SchemaNode
        {
            Name = "(root)",
            Type = SchemaNodeType.Object,
            Properties =
            [
                new SchemaNode { Name = "name", Type = SchemaNodeType.String, IsRequired = true },
            ],
        };

        var doc = new JsonObject { ["name"] = "hello" };

        var errors = _validator.Validate(schema, doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void Missing_required_property_reports_error()
    {
        var schema = new SchemaNode
        {
            Name = "(root)",
            Type = SchemaNodeType.Object,
            Properties =
            [
                new SchemaNode { Name = "name", Type = SchemaNodeType.String, IsRequired = true },
            ],
        };

        var doc = new JsonObject();

        var errors = _validator.Validate(schema, doc);

        Assert.Single(errors);
        Assert.Contains("name", errors[0].Path);
        Assert.Equal(ValidationSeverity.Error, errors[0].Severity);
    }

    [Fact]
    public void Enum_value_not_in_list_reports_error()
    {
        var schema = new SchemaNode
        {
            Name = "status",
            Type = SchemaNodeType.String,
            EnumValues = ["active", "inactive"],
        };

        var doc = JsonValue.Create("deleted");

        var errors = _validator.Validate(schema, doc);

        Assert.Single(errors);
        Assert.Contains("deleted", errors[0].Message);
    }

    [Fact]
    public void Valid_enum_value_passes()
    {
        var schema = new SchemaNode
        {
            Name = "status",
            Type = SchemaNodeType.String,
            EnumValues = ["active", "inactive"],
        };

        var doc = JsonValue.Create("active");

        var errors = _validator.Validate(schema, doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void String_below_minLength_reports_error()
    {
        var schema = new SchemaNode
        {
            Name = "code",
            Type = SchemaNodeType.String,
            MinLength = 3,
        };

        var doc = JsonValue.Create("ab");

        var errors = _validator.Validate(schema, doc);

        Assert.Single(errors);
        Assert.Contains("3", errors[0].Message);
    }

    [Fact]
    public void String_above_maxLength_reports_error()
    {
        var schema = new SchemaNode
        {
            Name = "code",
            Type = SchemaNodeType.String,
            MaxLength = 5,
        };

        var doc = JsonValue.Create("abcdef");

        var errors = _validator.Validate(schema, doc);

        Assert.Single(errors);
    }

    [Fact]
    public void String_matching_pattern_passes()
    {
        var schema = new SchemaNode
        {
            Name = "email",
            Type = SchemaNodeType.String,
            Pattern = @"^[^@]+@[^@]+$",
        };

        var doc = JsonValue.Create("test@example.com");

        var errors = _validator.Validate(schema, doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void String_not_matching_pattern_reports_error()
    {
        var schema = new SchemaNode
        {
            Name = "email",
            Type = SchemaNodeType.String,
            Pattern = @"^[^@]+@[^@]+$",
        };

        var doc = JsonValue.Create("no-at-sign");

        var errors = _validator.Validate(schema, doc);

        Assert.Single(errors);
        Assert.Contains("Muster", errors[0].Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Integer_below_minimum_reports_error()
    {
        var schema = new SchemaNode
        {
            Name = "age",
            Type = SchemaNodeType.Integer,
            Minimum = 0,
        };

        var doc = JsonValue.Create(-1);

        var errors = _validator.Validate(schema, doc);

        Assert.Single(errors);
    }

    [Fact]
    public void Integer_above_maximum_reports_error()
    {
        var schema = new SchemaNode
        {
            Name = "age",
            Type = SchemaNodeType.Integer,
            Maximum = 150,
        };

        var doc = JsonValue.Create(200);

        var errors = _validator.Validate(schema, doc);

        Assert.Single(errors);
    }

    [Fact]
    public void Array_below_minItems_reports_error()
    {
        var schema = new SchemaNode
        {
            Name = "items",
            Type = SchemaNodeType.Array,
            MinItems = 2,
            Items = new SchemaNode { Name = "(items)", Type = SchemaNodeType.String },
        };

        var doc = new JsonArray(JsonValue.Create("one"));

        var errors = _validator.Validate(schema, doc);

        Assert.Single(errors);
        Assert.Contains("2", errors[0].Message);
    }

    [Fact]
    public void Array_above_maxItems_reports_error()
    {
        var schema = new SchemaNode
        {
            Name = "items",
            Type = SchemaNodeType.Array,
            MaxItems = 1,
            Items = new SchemaNode { Name = "(items)", Type = SchemaNodeType.String },
        };

        var doc = new JsonArray(JsonValue.Create("a"), JsonValue.Create("b"));

        var errors = _validator.Validate(schema, doc);

        Assert.Single(errors);
    }

    [Fact]
    public void Const_mismatch_reports_error()
    {
        var schema = new SchemaNode
        {
            Name = "version",
            Type = SchemaNodeType.String,
            ConstValue = "1.0",
        };

        var doc = JsonValue.Create("2.0");

        var errors = _validator.Validate(schema, doc);

        Assert.Single(errors);
        Assert.Contains("1.0", errors[0].Message);
    }

    [Fact]
    public void Type_mismatch_object_expected_reports_error()
    {
        var schema = new SchemaNode
        {
            Name = "(root)",
            Type = SchemaNodeType.Object,
            Properties = [],
        };

        var doc = JsonValue.Create("not-an-object");

        var errors = _validator.Validate(schema, doc);

        Assert.Single(errors);
        Assert.Contains("Objekt", errors[0].Message);
    }

    [Fact]
    public void Unknown_property_reports_warning()
    {
        var schema = new SchemaNode
        {
            Name = "(root)",
            Type = SchemaNodeType.Object,
            Properties =
            [
                new SchemaNode { Name = "name", Type = SchemaNodeType.String },
            ],
        };

        var doc = new JsonObject { ["name"] = "ok", ["extra"] = "surprise" };

        var errors = _validator.Validate(schema, doc);

        Assert.Single(errors);
        Assert.Equal(ValidationSeverity.Warning, errors[0].Severity);
        Assert.Contains("extra", errors[0].Message);
    }

    [Fact]
    public void Nested_object_validates_recursively()
    {
        var schema = new SchemaNode
        {
            Name = "(root)",
            Type = SchemaNodeType.Object,
            Properties =
            [
                new SchemaNode
                {
                    Name = "address",
                    Type = SchemaNodeType.Object,
                    IsRequired = true,
                    Properties =
                    [
                        new SchemaNode { Name = "city", Type = SchemaNodeType.String, IsRequired = true },
                    ],
                },
            ],
        };

        var doc = new JsonObject { ["address"] = new JsonObject() };

        var errors = _validator.Validate(schema, doc);

        Assert.Single(errors);
        Assert.Contains("city", errors[0].Path);
    }

    [Fact]
    public void Map_validates_values_against_additionalProperties_schema()
    {
        var schema = new SchemaNode
        {
            Name = "(root)",
            Type = SchemaNodeType.Object,
            IsMap = true,
            AdditionalProperties = new SchemaNode
            {
                Name = "(value)",
                Type = SchemaNodeType.Integer,
                Minimum = 0,
            },
        };

        var doc = new JsonObject { ["a"] = 5, ["b"] = -1 };

        var errors = _validator.Validate(schema, doc);

        Assert.Single(errors);
        Assert.Contains("b", errors[0].Path);
    }

    [Fact]
    public void OneOf_matching_first_variant_passes()
    {
        var schema = new SchemaNode
        {
            Name = "val",
            Type = SchemaNodeType.OneOf,
            OneOf =
            [
                new SchemaNode { Name = "(option1)", Type = SchemaNodeType.String },
                new SchemaNode { Name = "(option2)", Type = SchemaNodeType.Integer },
            ],
        };

        var doc = JsonValue.Create("text");

        var errors = _validator.Validate(schema, doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void Null_document_for_required_field_reports_error()
    {
        var schema = new SchemaNode
        {
            Name = "field",
            Type = SchemaNodeType.String,
            IsRequired = true,
        };

        var errors = _validator.Validate(schema, null);

        Assert.Single(errors);
        Assert.Contains("Pflichtfeld", errors[0].Message);
    }

    [Fact]
    public void Null_document_for_optional_field_passes()
    {
        var schema = new SchemaNode
        {
            Name = "field",
            Type = SchemaNodeType.String,
            IsRequired = false,
        };

        var errors = _validator.Validate(schema, null);

        Assert.Empty(errors);
    }

    [Fact]
    public void Number_within_range_passes()
    {
        var schema = new SchemaNode
        {
            Name = "price",
            Type = SchemaNodeType.Number,
            Minimum = 0.0,
            Maximum = 1000.0,
        };

        var doc = JsonValue.Create(42.5);

        var errors = _validator.Validate(schema, doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void MinProperties_violation_reports_error()
    {
        var schema = new SchemaNode
        {
            Name = "(root)",
            Type = SchemaNodeType.Object,
            Properties =
            [
                new SchemaNode { Name = "a", Type = SchemaNodeType.String },
                new SchemaNode { Name = "b", Type = SchemaNodeType.String },
                new SchemaNode { Name = "c", Type = SchemaNodeType.String },
            ],
            MinProperties = 2,
        };

        var doc = new JsonObject { ["a"] = "x" };

        var errors = _validator.Validate(schema, doc);

        Assert.Single(errors);
        Assert.Contains("2", errors[0].Message);
    }

    [Fact]
    public void Array_items_validated_recursively()
    {
        var schema = new SchemaNode
        {
            Name = "tags",
            Type = SchemaNodeType.Array,
            Items = new SchemaNode { Name = "(items)", Type = SchemaNodeType.String, MinLength = 1 },
        };

        var doc = new JsonArray(JsonValue.Create("ok"), JsonValue.Create(""));

        var errors = _validator.Validate(schema, doc);

        Assert.Single(errors);
        Assert.Contains("[1]", errors[0].Path);
    }
}
