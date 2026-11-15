using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Xunit;

namespace Sprechen.Mda.Contracts.Tests.Schema;

/// <summary>
/// Prüft Serialisierung und Rücklesen aller bekannten ConstraintSeverity-Werte sowie
/// den beidseitigen Abgleich mit SeverityLevels. Die zugehörigen Schema-Tests
/// verbinden diese Konstanten mit dem JSON-Schema. Unbekannte Eingaben werden
/// auf Unknown abgebildet; das Schreiben dieses Werts muss fehlschlagen.
/// </summary>
public sealed class ConstraintSeverityEnumAlignmentTests
{
    public static IEnumerable<object[]> KnownSeverities()
        => Enum.GetValues<ConstraintSeverity>()
            .Where(s => s != ConstraintSeverity.Unknown)
            .Select(s => new object[] { s });

    [Theory]
    [MemberData(nameof(KnownSeverities))]
    public void EnumValue_SerializesAsItsWireToken_AndRoundTrips(ConstraintSeverity severity)
    {
        var wire = severity.ToWireToken();

        // Verwendet den am Enum registrierten JsonConverter ohne zusätzliche Testoptionen.
        Assert.Equal($"\"{wire}\"", JsonSerializer.Serialize(severity));
        Assert.Equal(severity, JsonSerializer.Deserialize<ConstraintSeverity>($"\"{wire}\""));

        // Der übertragene Wert muss aus SeverityLevels stammen; frei erfundene Werte sind unzulässig.
        Assert.Contains(wire, SeverityLevelsConstants());
    }

    [Fact]
    public void EnumWireValues_AreBidirectionallyAligned_WithSeverityLevelsConstants()
    {
        var enumWireValues = Enum.GetValues<ConstraintSeverity>()
            .Where(s => s != ConstraintSeverity.Unknown)
            .Select(s => s.ToWireToken())
            .ToHashSet(StringComparer.Ordinal);
        var constants = SeverityLevelsConstants();

        Assert.True(enumWireValues.SetEquals(constants),
            $"ConstraintSeverity wire values [{string.Join(", ", enumWireValues.OrderBy(v => v))}] " +
            $"must equal SeverityLevels constants [{string.Join(", ", constants.OrderBy(v => v))}].");

        foreach (var constant in constants)
        {
            Assert.True(ConstraintSeverityExtensions.TryFromWireToken(constant, out var severity),
                $"SeverityLevels constant '{constant}' must map back to a ConstraintSeverity.");
            Assert.Equal(constant, severity.ToWireToken());
        }
    }

    [Theory]
    [InlineData("\"Error\"")]   // Der PascalCase-Name des Enums ist kein zulässiger Übertragungswert.
    [InlineData("\"nonsense\"")]
    [InlineData("\"\"")]
    [InlineData("123")]
    [InlineData("true")]
    public void Read_TolerantlyMapsUnknownOrNonStringToUnknown_WithoutThrowing(string json)
    {
        Assert.Equal(ConstraintSeverity.Unknown, JsonSerializer.Deserialize<ConstraintSeverity>(json));
    }

    [Fact]
    public void Write_Unknown_Throws()
    {
        Assert.ThrowsAny<JsonException>(() => JsonSerializer.Serialize(ConstraintSeverity.Unknown));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Error")]
    [InlineData("nonsense")]
    public void TryFromWireToken_ReturnsFalseAndUnknown_ForNullWhitespaceOrUnknown(string? wire)
    {
        Assert.False(ConstraintSeverityExtensions.TryFromWireToken(wire, out var severity));
        Assert.Equal(ConstraintSeverity.Unknown, severity);
    }

    private static HashSet<string> SeverityLevelsConstants()
        => typeof(SeverityLevels)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);
}
