using System.Reflection;
using Sprechen.Common.Cqrs;
using Xunit;

namespace Sprechen.Common.Cqrs.Tests;

/// <summary>
/// Prüft, dass die direkt am Ereignisspeicher deklarierten öffentlichen Instanzmethoden
/// in seinen Speicherverträgen enthalten sind. Verglichen werden Name und Parametertypen,
/// damit Aufrufer keine zusätzlichen konkreten Methoden benötigen.
/// </summary>
public sealed class InMemoryEventStorePublicSurfaceTests
{
    [Fact]
    public void PublicMembers_AreDeclaredByEventStoreInterfaces()
    {
        var interfaceMembers = typeof(IEventStore).GetMethods()
            .Concat(typeof(ISynchronousEventStore).GetMethods())
            .Select(CreateSignature)
            .ToHashSet(StringComparer.Ordinal);

        var concreteOnlyMembers = typeof(InMemoryEventStore)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(CreateSignature)
            .Except(interfaceMembers, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(concreteOnlyMembers);
    }

    private static string CreateSignature(MethodInfo method)
    {
        var parameterTypes = method.GetParameters()
            .Select(parameter => parameter.ParameterType.FullName ?? parameter.ParameterType.Name);

        return $"{method.Name}({string.Join(",", parameterTypes)})";
    }
}
