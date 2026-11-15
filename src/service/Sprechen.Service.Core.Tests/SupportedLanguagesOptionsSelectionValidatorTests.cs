using Microsoft.Extensions.Options;
using Sprechen.Service.Core.Services;
using Xunit;

namespace Sprechen.Service.Core.Tests;

public sealed class SupportedLanguagesOptionsSelectionValidatorTests
{
    [Fact]
    public void Validate_WhenIsoCodesAreEmpty_AcceptsCatalogDefaultRule()
    {
        var result = CreateValidator().Validate(null, new SupportedLanguagesOptions
        {
            DefaultIsoCode = "de",
            IsoCodes = Array.Empty<string>()
        });

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void Validate_WhenIsoCodesOmitDefault_AcceptsCatalogInsertionRule()
    {
        var result = CreateValidator().Validate(null, new SupportedLanguagesOptions
        {
            DefaultIsoCode = "de",
            IsoCodes = new[] { "en" }
        });

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void Validate_WhenIsoCodeIsNotAllowed_Fails()
    {
        var result = CreateValidator().Validate(null, new SupportedLanguagesOptions
        {
            DefaultIsoCode = "de",
            IsoCodes = new[] { "de", "pt" }
        });

        Assert.False(result.Succeeded);
    }

    private static SupportedLanguagesOptionsSelectionValidator CreateValidator()
        => new();
}
