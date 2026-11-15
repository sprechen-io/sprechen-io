using System.Reflection;
using Sprechen.Speech.WakeWord.Contracts;
using Xunit;

namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Prüft, dass die öffentliche statische Aktivierungsmethode Text, Konfidenz und
/// das Wake-Word-Inventar verlangt.
/// </summary>
public sealed class WakeWordActivationRuleHasOnlyInventoryAwareOverloadTests
{
    [Fact]
    public void EvaluateLeadingWakeWord_HasExactlyOnePublicOverloadWithInventoryParameter()
    {
        var methods = typeof(WakeWordActivationRule)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == "EvaluateLeadingWakeWord")
            .ToArray();

        Assert.Single(methods);

        var parameters = methods[0].GetParameters();
        Assert.Equal(3, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(int?), parameters[1].ParameterType);
        Assert.Equal(typeof(WakeWordInventory), parameters[2].ParameterType);
    }
}
