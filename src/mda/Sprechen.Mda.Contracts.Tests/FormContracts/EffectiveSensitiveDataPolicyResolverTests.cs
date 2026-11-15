using Sprechen.Mda.Contracts.V1.FormContracts;
using Xunit;

namespace Sprechen.Mda.Contracts.Tests.FormContracts;

public sealed class EffectiveSensitiveDataPolicyResolverTests
{
    private readonly EffectiveSensitiveDataPolicyResolver _resolver = new();

    [Fact]
    public void TryResolve_WhenPrivacyAbsent_UsesPublicDefaults()
    {
        var ok = _resolver.TryResolve(null, null, out var policy, out var failure);

        Assert.True(ok);
        Assert.Equal(EffectiveSensitiveDataPolicyFailure.None, failure);
        Assert.NotNull(policy);
        Assert.Equal(PrivacyClasses.Public, policy.PrivacyClass);
        Assert.Equal(SensitiveDataPolicyDefaults.VoiceInputRecommended, policy.VoiceInputPolicy);
        Assert.Equal(SensitiveDataPolicyDefaults.ReadbackAlways, policy.ReadbackPolicy);
        Assert.True(policy.AllowsVoiceInput);
        Assert.True(policy.AllowsReadback);
        Assert.True(policy.AllowsSpelledReadback);
        Assert.False(policy.OffersSilentInput);
    }

    [Fact]
    public void TryResolve_WhenUnknownPrivacyPresent_Fails()
    {
        var ok = _resolver.TryResolve(PrivacyClass.Unknown, null, out var policy, out var failure);

        Assert.False(ok);
        Assert.Null(policy);
        Assert.Equal(EffectiveSensitiveDataPolicyFailure.UnknownPrivacyClass, failure);
    }

    [Fact]
    public void TryResolve_WhenSecretVoiceIsNotForbidden_Fails()
    {
        var ok = _resolver.TryResolve(
            PrivacyClass.Secret,
            new SensitiveDataPolicy { VoiceInputPolicy = SensitiveDataPolicyDefaults.VoiceInputOptional },
            out var policy,
            out var failure);

        Assert.False(ok);
        Assert.Null(policy);
        Assert.Equal(EffectiveSensitiveDataPolicyFailure.SecretVoiceInputNotForbidden, failure);
    }

    [Fact]
    public void TryResolve_WhenSensitiveReadbackIsAlways_Fails()
    {
        var ok = _resolver.TryResolve(
            PrivacyClass.Sensitive,
            new SensitiveDataPolicy { ReadbackPolicy = SensitiveDataPolicyDefaults.ReadbackAlways },
            out var policy,
            out var failure);

        Assert.False(ok);
        Assert.Null(policy);
        Assert.Equal(EffectiveSensitiveDataPolicyFailure.SensitiveReadbackAlways, failure);
    }

    [Fact]
    public void TryResolve_WhenBlankReadbackPresent_FailsBeforeContradiction()
    {
        var ok = _resolver.TryResolve(
            PrivacyClass.Secret,
            new SensitiveDataPolicy
            {
                VoiceInputPolicy = SensitiveDataPolicyDefaults.VoiceInputOptional,
                ReadbackPolicy = "   "
            },
            out var policy,
            out var failure);

        Assert.False(ok);
        Assert.Null(policy);
        Assert.Equal(EffectiveSensitiveDataPolicyFailure.BlankReadbackPolicy, failure);
    }

    [Fact]
    public void TryResolve_WhenSensitiveAbsentPolicy_UsesModeledDefaults()
    {
        var ok = _resolver.TryResolve(PrivacyClass.Sensitive, null, out var policy, out var failure);

        Assert.True(ok);
        Assert.Equal(EffectiveSensitiveDataPolicyFailure.None, failure);
        Assert.NotNull(policy);
        Assert.Equal(PrivacyClasses.Sensitive, policy.PrivacyClass);
        Assert.Equal(SensitiveDataPolicyDefaults.VoiceInputOptional, policy.VoiceInputPolicy);
        Assert.Equal(SensitiveDataPolicyDefaults.ReadbackOptInRequired, policy.ReadbackPolicy);
        Assert.True(policy.AllowsVoiceInput);
        Assert.True(policy.RequiresReadbackOptIn);
        Assert.True(policy.AllowsReadback);
        Assert.True(policy.OffersSilentInput);
        Assert.False(policy.AllowsSpelledReadback);
    }

    [Fact]
    public void TryResolve_WhenSecretAbsentPolicy_UsesModeledDefaults()
    {
        var ok = _resolver.TryResolve(PrivacyClass.Secret, null, out var policy, out var failure);

        Assert.True(ok);
        Assert.Equal(EffectiveSensitiveDataPolicyFailure.None, failure);
        Assert.NotNull(policy);
        Assert.Equal(PrivacyClasses.Secret, policy.PrivacyClass);
        Assert.Equal(SensitiveDataPolicyDefaults.VoiceInputForbidden, policy.VoiceInputPolicy);
        Assert.Equal(SensitiveDataPolicyDefaults.ReadbackNever, policy.ReadbackPolicy);
        Assert.False(policy.AllowsVoiceInput);
        Assert.False(policy.AllowsReadback);
        Assert.True(policy.OffersSilentInput);
        Assert.False(policy.AllowsSpelledReadback);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TryResolve_WhenBlankVoiceInputPresent_Fails(string blank)
    {
        var ok = _resolver.TryResolve(
            PrivacyClass.Public,
            new SensitiveDataPolicy { VoiceInputPolicy = blank },
            out var policy,
            out var failure);

        Assert.False(ok);
        Assert.Null(policy);
        Assert.Equal(EffectiveSensitiveDataPolicyFailure.BlankVoiceInputPolicy, failure);
    }

    [Fact]
    public void TryResolve_WhenUnsupportedReadbackPresent_Fails()
    {
        var ok = _resolver.TryResolve(
            PrivacyClass.Public,
            new SensitiveDataPolicy { ReadbackPolicy = "sometimes" },
            out var policy,
            out var failure);

        Assert.False(ok);
        Assert.Null(policy);
        Assert.Equal(EffectiveSensitiveDataPolicyFailure.UnsupportedReadbackPolicy, failure);
    }

    [Fact]
    public void TryResolve_WhenSecretReadbackIsNotNever_Fails()
    {
        var ok = _resolver.TryResolve(
            PrivacyClass.Secret,
            new SensitiveDataPolicy { ReadbackPolicy = SensitiveDataPolicyDefaults.ReadbackOptInRequired },
            out var policy,
            out var failure);

        Assert.False(ok);
        Assert.Null(policy);
        Assert.Equal(EffectiveSensitiveDataPolicyFailure.SecretReadbackNotNever, failure);
    }

    [Fact]
    public void FormContractPolicyInvalidException_CarriesFailureWithoutAuthoredToken()
    {
        var exception = new FormContractPolicyInvalidException(
            EffectiveSensitiveDataPolicyFailure.UnknownPrivacyClass);

        Assert.Equal(FormContractPolicyInvalidException.ErrorCode, "FORM_CONTRACT_POLICY_INVALID");
        Assert.Equal(EffectiveSensitiveDataPolicyFailure.UnknownPrivacyClass, exception.Failure);
        Assert.DoesNotContain("unknown", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
