using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.Tests.Schema;

public sealed class ConstantSchemaAlignmentTests
{
    /// <summary>
    /// Vergleicht die deklarierten Interaktionstypen mit den Resolver-Ergebnissen für
    /// alle deklarierten Werttypen und beide codeStyle-Einstellungen.
    /// Für Interaktionstypen existiert kein direktes Schema-Enum.
    /// </summary>
    [Fact]
    public void EffectiveFieldInteractionTypes_AreDerivedFromValueTypeKinds_Bidirectional()
    {
        var declaredInteractionTypes = GetPublicConstStringValues(typeof(EffectiveFieldInteractionTypes));
        var valueTypeKindValues = GetPublicConstStringValues(typeof(ValueTypeKinds));

        var reachableInteractionTypes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var vtk in valueTypeKindValues)
        {
            reachableInteractionTypes.Add(
                FieldInteractionSemantics.ResolveEffectiveInteractionType(codeStyle: false, vtk));
            reachableInteractionTypes.Add(
                FieldInteractionSemantics.ResolveEffectiveInteractionType(codeStyle: true, vtk));
        }

        var unreachable = declaredInteractionTypes
            .Except(reachableInteractionTypes, StringComparer.Ordinal)
            .OrderBy(x => x)
            .ToArray();

        var undeclared = reachableInteractionTypes
            .Except(declaredInteractionTypes, StringComparer.Ordinal)
            .OrderBy(x => x)
            .ToArray();

        if (unreachable.Length > 0 || undeclared.Length > 0)
        {
            throw new Xunit.Sdk.XunitException(
                $"Derived drift detected between {nameof(EffectiveFieldInteractionTypes)} and " +
                $"{nameof(FieldInteractionSemantics)}.{nameof(FieldInteractionSemantics.ResolveEffectiveInteractionType)}.\n" +
                $"Declared but unreachable from any ValueTypeKind: [{string.Join(", ", unreachable)}]\n" +
                $"Produced by resolver but not declared: [{string.Join(", ", undeclared)}]");
        }
    }

    /// <summary>
    /// Liest ausschließlich öffentliche const-string-Felder der statischen Klasse;
    /// Methoden, Eigenschaften und veränderliche Felder bleiben unberücksichtigt.
    /// </summary>
    internal static HashSet<string> GetPublicConstStringValues(Type type)
    {
        return type
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToHashSet(StringComparer.Ordinal);
    }
}
