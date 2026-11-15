using System.Reflection;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Xunit;

namespace Sprechen.Mda.Contracts.Tests.FormContracts;

public sealed class SensitiveDataPolicyDefaultsOwnerTests
{
    [Fact]
    public void SensitiveDataPolicyDefaults_ExposesConstantsOnly()
    {
        var publicMethods = typeof(SensitiveDataPolicyDefaults)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .ToArray();

        Assert.Empty(publicMethods);
    }
}
