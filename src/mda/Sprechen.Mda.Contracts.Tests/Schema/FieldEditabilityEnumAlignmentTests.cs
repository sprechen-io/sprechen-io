using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Xunit;

namespace Sprechen.Mda.Contracts.Tests.Schema;

/// <summary>
/// Prüft Serialisierung und Rücklesen aller bekannten FieldEditability-Werte sowie
/// den beidseitigen Abgleich mit EditabilityModes und dadurch mit den Schemawerten.
/// Unbekannte Eingaben werden auf Unknown abgebildet; Schreiben von Unknown schlägt fehl.
/// </summary>
public sealed class FieldEditabilityEnumAlignmentTests
{
    public static IEnumerable<object[]> KnownModes()
        => Enum.GetValues<FieldEditability>()
            .Where(m => m != FieldEditability.Unknown)
            .Select(m => new object[] { m });

    [Theory]
    [MemberData(nameof(KnownModes))]
    public void EnumValue_SerializesAsItsWireToken_AndRoundTrips(FieldEditability editability)
    {
        var wire = editability.ToWireToken();

        // Verwendet den am Enum registrierten JsonConverter ohne zusätzliche Testoptionen.
        Assert.Equal($"\"{wire}\"", JsonSerializer.Serialize(editability));
        Assert.Equal(editability, JsonSerializer.Deserialize<FieldEditability>($"\"{wire}\""));

        // Der übertragene Wert muss aus EditabilityModes stammen; frei erfundene Werte sind unzulässig.
        Assert.Contains(wire, EditabilityModesConstants());
    }

    [Fact]
    public void EnumWireValues_AreBidirectionallyAligned_WithEditabilityModesConstants()
    {
        var enumWireValues = Enum.GetValues<FieldEditability>()
            .Where(m => m != FieldEditability.Unknown)
            .Select(m => m.ToWireToken())
            .ToHashSet(StringComparer.Ordinal);
        var constants = EditabilityModesConstants();

        Assert.True(enumWireValues.SetEquals(constants),
            $"FieldEditability wire values [{string.Join(", ", enumWireValues.OrderBy(v => v))}] " +
            $"must equal EditabilityModes constants [{string.Join(", ", constants.OrderBy(v => v))}].");

        foreach (var constant in constants)
        {
            Assert.True(FieldEditabilityExtensions.TryFromWireToken(constant, out var editability),
                $"EditabilityModes constant '{constant}' must map back to a FieldEditability.");
            Assert.Equal(constant, editability.ToWireToken());
        }
    }

    [Theory]
    [InlineData("\"UserEditable\"")]   // Der PascalCase-Name des Enums ist kein zulässiger Übertragungswert.
    [InlineData("\"nonsense\"")]
    [InlineData("\"\"")]
    [InlineData("123")]
    [InlineData("true")]
    public void Read_TolerantlyMapsUnknownOrNonStringToUnknown_WithoutThrowing(string json)
    {
        Assert.Equal(FieldEditability.Unknown, JsonSerializer.Deserialize<FieldEditability>(json));
    }

    [Fact]
    public void Write_Unknown_Throws()
    {
        Assert.ThrowsAny<JsonException>(() => JsonSerializer.Serialize(FieldEditability.Unknown));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("UserEditable")]
    [InlineData("nonsense")]
    public void TryFromWireToken_ReturnsFalseAndUnknown_ForNullWhitespaceOrUnknown(string? wire)
    {
        Assert.False(FieldEditabilityExtensions.TryFromWireToken(wire, out var editability));
        Assert.Equal(FieldEditability.Unknown, editability);
    }

    private static HashSet<string> EditabilityModesConstants()
        => typeof(EditabilityModes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);
}
