using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Xunit;

namespace Sprechen.Mda.Contracts.Tests.Schema;

/// <summary>
/// Prüft Serialisierung und Rücklesen bekannter PrivacyClass-Werte sowie den
/// beidseitigen Vergleich mit PrivacyClasses und darüber mit den Schemawerten.
/// Unbekannte Eingaben ergeben Unknown; dessen Serialisierung schlägt fehl.
/// </summary>
public sealed class PrivacyClassEnumAlignmentTests
{
    public static IEnumerable<object[]> KnownClasses()
        => Enum.GetValues<PrivacyClass>()
            .Where(c => c != PrivacyClass.Unknown)
            .Select(c => new object[] { c });

    [Theory]
    [MemberData(nameof(KnownClasses))]
    public void EnumValue_SerializesAsItsWireToken_AndRoundTrips(PrivacyClass privacyClass)
    {
        var wire = privacyClass.ToWireToken();

        // Verwendet den am Enum registrierten JsonConverter ohne zusätzliche Testoptionen.
        Assert.Equal($"\"{wire}\"", JsonSerializer.Serialize(privacyClass));
        Assert.Equal(privacyClass, JsonSerializer.Deserialize<PrivacyClass>($"\"{wire}\""));

        // Der übertragene Wert muss aus PrivacyClasses stammen; frei erfundene Werte sind unzulässig.
        Assert.Contains(wire, PrivacyClassesConstants());
    }

    [Fact]
    public void EnumWireValues_AreBidirectionallyAligned_WithPrivacyClassesConstants()
    {
        var enumWireValues = Enum.GetValues<PrivacyClass>()
            .Where(c => c != PrivacyClass.Unknown)
            .Select(c => c.ToWireToken())
            .ToHashSet(StringComparer.Ordinal);
        var constants = PrivacyClassesConstants();

        Assert.True(enumWireValues.SetEquals(constants),
            $"PrivacyClass wire values [{string.Join(", ", enumWireValues.OrderBy(v => v))}] " +
            $"must equal PrivacyClasses constants [{string.Join(", ", constants.OrderBy(v => v))}].");

        foreach (var constant in constants)
        {
            Assert.True(PrivacyClassExtensions.TryFromWireToken(constant, out var privacyClass),
                $"PrivacyClasses constant '{constant}' must map back to a PrivacyClass.");
            Assert.Equal(constant, privacyClass.ToWireToken());
        }
    }

    [Theory]
    [InlineData("\"Secret\"")]   // Der PascalCase-Name des Enums ist kein zulässiger Übertragungswert.
    [InlineData("\"nonsense\"")]
    [InlineData("\"\"")]
    [InlineData("123")]
    [InlineData("true")]
    public void Read_TolerantlyMapsUnknownOrNonStringToUnknown_WithoutThrowing(string json)
    {
        Assert.Equal(PrivacyClass.Unknown, JsonSerializer.Deserialize<PrivacyClass>(json));
    }

    [Fact]
    public void Write_Unknown_Throws()
    {
        Assert.ThrowsAny<JsonException>(() => JsonSerializer.Serialize(PrivacyClass.Unknown));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Secret")]
    [InlineData("nonsense")]
    public void TryFromWireToken_ReturnsFalseAndUnknown_ForNullWhitespaceOrUnknown(string? wire)
    {
        Assert.False(PrivacyClassExtensions.TryFromWireToken(wire, out var privacyClass));
        Assert.Equal(PrivacyClass.Unknown, privacyClass);
    }

    private static HashSet<string> PrivacyClassesConstants()
        => typeof(PrivacyClasses)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);
}
