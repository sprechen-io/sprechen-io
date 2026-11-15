using System.Text.Json;

namespace Sprechen.Contracts.Tests.FormSurfaces.Projection;

/// <summary>
/// Lädt gemeinsame Testfälle zur Regelprojektion, damit beide Oberflächen dieselben Erwartungen prüfen können.
/// </summary>
internal static class ContractRuleEvaluationTable
{
    public static IReadOnlyList<JsonElement> EnforcedRuleProjectionCases()
    {
        using var document = ReferenceTableResources.Read("contract-rule-evaluation.reference.json");
        return document.RootElement
            .GetProperty("enforcedRuleProjectionCases")
            .EnumerateArray()
            .Select(row => row.Clone())
            .ToList();
    }

}
