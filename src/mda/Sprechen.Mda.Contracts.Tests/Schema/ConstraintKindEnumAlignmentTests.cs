using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Xunit;

namespace Sprechen.Mda.Contracts.Tests.Schema;

/// <summary>
/// Prüft Serialisierung und Rücklesen aller bekannten ConstraintKind-Werte sowie
/// den beidseitigen Abgleich mit ConstraintKinds. Die zugehörigen Schema-Tests
/// verbinden diese Konstanten mit dem JSON-Schema. Unbekannte Eingaben werden
/// auf Unknown abgebildet; das Schreiben dieses Werts muss fehlschlagen.
/// </summary>
public sealed class ConstraintKindEnumAlignmentTests
{
    public static IEnumerable<object[]> KnownKinds()
        => Enum.GetValues<ConstraintKind>()
            .Where(k => k != ConstraintKind.Unknown)
            .Select(k => new object[] { k });

    [Theory]
    [MemberData(nameof(KnownKinds))]
    public void EnumValue_SerializesAsItsWireToken_AndRoundTrips(ConstraintKind kind)
    {
        var wire = kind.ToWireToken();

        // Verwendet den am Enum registrierten JsonConverter ohne zusätzliche Testoptionen.
        Assert.Equal($"\"{wire}\"", JsonSerializer.Serialize(kind));
        Assert.Equal(kind, JsonSerializer.Deserialize<ConstraintKind>($"\"{wire}\""));

        // Der übertragene Wert muss aus ConstraintKinds stammen; frei erfundene Werte sind unzulässig.
        Assert.Contains(wire, ConstraintKindsConstants());
    }

    [Fact]
    public void EnumWireValues_AreBidirectionallyAligned_WithConstraintKindsConstants()
    {
        var enumWireValues = Enum.GetValues<ConstraintKind>()
            .Where(k => k != ConstraintKind.Unknown)
            .Select(k => k.ToWireToken())
            .ToHashSet(StringComparer.Ordinal);
        var constants = ConstraintKindsConstants();

        Assert.True(enumWireValues.SetEquals(constants),
            $"ConstraintKind wire values [{string.Join(", ", enumWireValues.OrderBy(v => v))}] " +
            $"must equal ConstraintKinds constants [{string.Join(", ", constants.OrderBy(v => v))}].");

        foreach (var constant in constants)
        {
            Assert.True(ConstraintKindExtensions.TryFromWireToken(constant, out var kind),
                $"ConstraintKinds constant '{constant}' must map back to a ConstraintKind.");
            Assert.Equal(constant, kind.ToWireToken());
        }
    }

    [Theory]
    [InlineData("\"Required\"")]   // Der PascalCase-Name des Enums ist kein zulässiger Übertragungswert.
    [InlineData("\"nonsense\"")]
    [InlineData("\"\"")]
    [InlineData("123")]
    [InlineData("true")]
    public void Read_TolerantlyMapsUnknownOrNonStringToUnknown_WithoutThrowing(string json)
    {
        Assert.Equal(ConstraintKind.Unknown, JsonSerializer.Deserialize<ConstraintKind>(json));
    }

    [Fact]
    public void Write_Unknown_Throws()
    {
        Assert.ThrowsAny<JsonException>(() => JsonSerializer.Serialize(ConstraintKind.Unknown));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Required")]
    [InlineData("nonsense")]
    public void TryFromWireToken_ReturnsFalseAndUnknown_ForNullWhitespaceOrUnknown(string? wire)
    {
        Assert.False(ConstraintKindExtensions.TryFromWireToken(wire, out var kind));
        Assert.Equal(ConstraintKind.Unknown, kind);
    }

    private static HashSet<string> ConstraintKindsConstants()
        => typeof(ConstraintKinds)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);
}
