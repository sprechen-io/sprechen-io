using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Xunit;

namespace Sprechen.Mda.Contracts.Tests.Schema;

/// <summary>
/// Prüft Serialisierung und Rücklesen der InputMode-Werte sowie die Ablehnung
/// unbekannter Eingaben. Der beidseitige Vergleich mit InputModes ergänzt deren
/// Schemaabgleich; Übertragungswerte kommen aus dem Konverter statt aus Enum-Namen.
/// </summary>
public sealed class InputModeEnumAlignmentTests
{
    public static IEnumerable<object[]> AllModes()
        => Enum.GetValues<InputMode>().Select(m => new object[] { m });

    [Theory]
    [MemberData(nameof(AllModes))]
    public void EnumValue_SerializesAsItsWireString_AndRoundTrips(InputMode mode)
    {
        var wire = mode.ToWireValue();

        // Verwendet den am Enum registrierten JsonConverter ohne zusätzliche Testoptionen.
        Assert.Equal($"\"{wire}\"", JsonSerializer.Serialize(mode));
        Assert.Equal(mode, JsonSerializer.Deserialize<InputMode>($"\"{wire}\""));

        // Der übertragene Wert muss aus InputModes stammen; frei erfundene Werte sind unzulässig.
        Assert.Contains(wire, InputModesConstants());
    }

    [Fact]
    public void EnumWireValues_AreBidirectionallyAligned_WithInputModesConstants()
    {
        var enumWireValues = Enum.GetValues<InputMode>()
            .Select(m => m.ToWireValue())
            .ToHashSet(StringComparer.Ordinal);
        var constants = InputModesConstants();

        // Der Vergleich muss fehlende und zusätzliche Übertragungswerte erkennen.
        Assert.True(enumWireValues.SetEquals(constants),
            $"InputMode wire values [{string.Join(", ", enumWireValues.OrderBy(v => v))}] " +
            $"must equal InputModes constants [{string.Join(", ", constants.OrderBy(v => v))}].");

        foreach (var constant in constants)
        {
            Assert.True(InputModeExtensions.TryFromWireValue(constant, out var mode),
                $"InputModes constant '{constant}' must map back to an InputMode.");
            Assert.Equal(constant, mode.ToWireValue());
        }
    }

    [Theory]
    [InlineData("\"Voice\"")]   // Der PascalCase-Name des Enums ist kein zulässiger Übertragungswert.
    [InlineData("\"unknown\"")]
    [InlineData("\"\"")]
    [InlineData("123")]
    [InlineData("true")]
    public void Read_FailsClosed_OnNonStringOrUnknownValue(string json)
    {
        Assert.ThrowsAny<JsonException>(() => JsonSerializer.Deserialize<InputMode>(json));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Voice")]
    [InlineData("nonsense")]
    public void TryFromWireValue_RejectsNullWhitespaceAndUnknown(string? wire)
    {
        Assert.False(InputModeExtensions.TryFromWireValue(wire, out _));
    }

    private static HashSet<string> InputModesConstants()
        => typeof(InputModes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);
}
