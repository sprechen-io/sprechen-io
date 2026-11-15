using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Xunit;

namespace Sprechen.Mda.Contracts.Tests.Schema;

/// <summary>
/// Prüft Serialisierung und Rücklesen aller bekannten ConstraintScope-Werte sowie
/// den beidseitigen Abgleich mit ConstraintScopes. Die zugehörigen Schema-Tests
/// verbinden diese Konstanten mit dem JSON-Schema. Unbekannte Eingaben werden
/// auf Unknown abgebildet; das Schreiben dieses Werts muss fehlschlagen.
/// </summary>
public sealed class ConstraintScopeEnumAlignmentTests
{
    public static IEnumerable<object[]> KnownScopes()
        => Enum.GetValues<ConstraintScope>()
            .Where(s => s != ConstraintScope.Unknown)
            .Select(s => new object[] { s });

    [Theory]
    [MemberData(nameof(KnownScopes))]
    public void EnumValue_SerializesAsItsWireToken_AndRoundTrips(ConstraintScope scope)
    {
        var wire = scope.ToWireToken();

        // Verwendet den am Enum registrierten JsonConverter ohne zusätzliche Testoptionen.
        Assert.Equal($"\"{wire}\"", JsonSerializer.Serialize(scope));
        Assert.Equal(scope, JsonSerializer.Deserialize<ConstraintScope>($"\"{wire}\""));

        // Der übertragene Wert muss aus ConstraintScopes stammen; frei erfundene Werte sind unzulässig.
        Assert.Contains(wire, ConstraintScopesConstants());
    }

    [Fact]
    public void EnumWireValues_AreBidirectionallyAligned_WithConstraintScopesConstants()
    {
        var enumWireValues = Enum.GetValues<ConstraintScope>()
            .Where(s => s != ConstraintScope.Unknown)
            .Select(s => s.ToWireToken())
            .ToHashSet(StringComparer.Ordinal);
        var constants = ConstraintScopesConstants();

        Assert.True(enumWireValues.SetEquals(constants),
            $"ConstraintScope wire values [{string.Join(", ", enumWireValues.OrderBy(v => v))}] " +
            $"must equal ConstraintScopes constants [{string.Join(", ", constants.OrderBy(v => v))}].");

        foreach (var constant in constants)
        {
            Assert.True(ConstraintScopeExtensions.TryFromWireToken(constant, out var scope),
                $"ConstraintScopes constant '{constant}' must map back to a ConstraintScope.");
            Assert.Equal(constant, scope.ToWireToken());
        }
    }

    [Theory]
    [InlineData("\"Field\"")]   // Der PascalCase-Name des Enums ist kein zulässiger Übertragungswert.
    [InlineData("\"groupOnly\"")]
    [InlineData("\"\"")]
    [InlineData("123")]
    [InlineData("true")]
    public void Read_TolerantlyMapsUnknownOrNonStringToUnknown_WithoutThrowing(string json)
    {
        Assert.Equal(ConstraintScope.Unknown, JsonSerializer.Deserialize<ConstraintScope>(json));
    }

    [Fact]
    public void Write_Unknown_Throws()
    {
        Assert.ThrowsAny<JsonException>(() => JsonSerializer.Serialize(ConstraintScope.Unknown));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Field")]
    [InlineData("groupOnly")]
    public void TryFromWireToken_ReturnsFalseAndUnknown_ForNullWhitespaceOrUnknown(string? wire)
    {
        Assert.False(ConstraintScopeExtensions.TryFromWireToken(wire, out var scope));
        Assert.Equal(ConstraintScope.Unknown, scope);
    }

    private static HashSet<string> ConstraintScopesConstants()
        => typeof(ConstraintScopes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);
}
