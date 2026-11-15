using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormPrivacy;

internal static class FormPrivacyTestFixture
{
    public static FormPrivacyReadbackPolicy CreatePolicyWithVariables()
    {
        var variables = new Dictionary<string, VariableDefinition>(StringComparer.Ordinal)
        {
            ["publicVar"] = new()
            {
                VariableId = VariableId.From("publicVar"),
                Name = "publicVar",
                PrivacyClass = PrivacyClass.Public
            },
            ["personalVar"] = new()
            {
                VariableId = VariableId.From("personalVar"),
                Name = "personalVar",
                PrivacyClass = PrivacyClass.Personal
            },
            ["sensitiveVar"] = new()
            {
                VariableId = VariableId.From("sensitiveVar"),
                Name = "sensitiveVar",
                PrivacyClass = PrivacyClass.Sensitive
            },
            ["secretVar"] = new()
            {
                VariableId = VariableId.From("secretVar"),
                Name = "secretVar",
                PrivacyClass = PrivacyClass.Secret
            }
        };

        return new FormPrivacyReadbackPolicy(ResolvePolicies(variables.Values));
    }

    public static FormPrivacyReadbackPolicy CreateEmptyPolicy()
        => new(ImmutableDictionary<VariableId, EffectiveSensitiveDataPolicy>.Empty);

    public static ImmutableDictionary<VariableId, EffectiveSensitiveDataPolicy> ResolvePolicies(
        IEnumerable<VariableDefinition> variables)
    {
        var resolver = new EffectiveSensitiveDataPolicyResolver();
        var builder = ImmutableDictionary.CreateBuilder<VariableId, EffectiveSensitiveDataPolicy>();
        foreach (var variable in variables)
        {
            if (!resolver.TryResolve(variable.PrivacyClass, variable.SensitiveDataPolicy, out var policy, out var failure))
            {
                throw new InvalidOperationException(failure.ToString());
            }

            builder.Add(variable.VariableId, policy!);
        }

        return builder.ToImmutable();
    }
}
