using Sprechen.Service.Deepgram.Options;
using Xunit;

namespace Sprechen.Service.Deepgram.Tests;

public sealed class OptionsSelectionValidatorTests
{
    [Fact]
    public void Validate_WhenGlobalEndpointConfigured_Fails()
    {
        var validator = CreateValidator(CreateSupportedLanguages("en", "en"));

        var result = validator.Validate(
            name: null,
            new DeepgramOptions
            {
                ApiKey = "k",
                Endpoint = "https://api.deepgram.com/",
                TtsEndpoint = "https://api.deepgram.com/",
                TtsModels = new Dictionary<string, string>
                {
                    ["en"] = "aura-2-helena-en"
                }
            });

        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_TtsEndpoint_DefaultsToEuHost()
    {
        var validator = CreateValidator(CreateSupportedLanguages("de", "de", "en"));

        var result = validator.Validate(name: null, CreateValidOptions(CreateDefaultTtsModels()));

        Assert.False(result.Failed, string.Join("; ", result.Failures ?? Array.Empty<string>()));
    }

    [Fact]
    public void Validate_TtsEndpoint_NonEuHostRejectedWithoutOptOut()
    {
        var validator = CreateValidator(CreateSupportedLanguages("de", "de", "en"));

        var options = CreateValidOptionsWithTtsEndpoint(
            ttsEndpoint: "https://api.deepgram.com/",
            ttsAllowNonEuEndpoint: false);

        var result = validator.Validate(name: null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure =>
            failure.Contains("TtsEndpoint", StringComparison.Ordinal)
            && failure.Contains("TtsAllowNonEuEndpoint", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_TtsEndpoint_NonEuHostAcceptedWithExplicitOptOut()
    {
        var validator = CreateValidator(CreateSupportedLanguages("de", "de", "en"));

        var options = CreateValidOptionsWithTtsEndpoint(
            ttsEndpoint: "https://api.deepgram.com/",
            ttsAllowNonEuEndpoint: true);

        var result = validator.Validate(name: null, options);

        Assert.False(result.Failed, string.Join("; ", result.Failures ?? Array.Empty<string>()));
    }

    [Fact]
    public void Validate_TtsEndpoint_OptOutWithInvalidHostStillFails()
    {
        var validator = CreateValidator(CreateSupportedLanguages("de", "de", "en"));

        var options = CreateValidOptionsWithTtsEndpoint(
            ttsEndpoint: "https://example.com/",
            ttsAllowNonEuEndpoint: true);

        var result = validator.Validate(name: null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("TtsEndpoint", StringComparison.Ordinal));
    }

    private static DeepgramOptions CreateValidOptionsWithTtsEndpoint(
        string ttsEndpoint,
        bool ttsAllowNonEuEndpoint)
    {
        return new DeepgramOptions
        {
            ApiKey = "k",
            Endpoint = "https://api.eu.deepgram.com/",
            TtsEndpoint = ttsEndpoint,
            TtsAllowNonEuEndpoint = ttsAllowNonEuEndpoint,
            TtsModels = CreateDefaultTtsModels()
        };
    }

    [Fact]
    public void Validate_WhenDeepgramTtsSelectedAndModelMissingForSupportedLanguage_Fails()
    {
        var validator = CreateValidator(CreateSupportedLanguages("de", "de", "en"));

        var options = CreateValidOptions(new Dictionary<string, string>
        {
            ["de"] = "aura-2-aurelia-de"
        });

        var result = validator.Validate(name: null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("TtsModels", StringComparison.Ordinal));
    }

    private static OptionsSelectionValidator CreateValidator(
        SupportedLanguagesOptions supportedLanguagesOptions)
    {
        return new OptionsSelectionValidator(
            Microsoft.Extensions.Options.Options.Create(supportedLanguagesOptions));
    }

    private static SupportedLanguagesOptions CreateSupportedLanguages(string defaultIsoCode, params string[] isoCodes)
    {
        return new SupportedLanguagesOptions
        {
            DefaultIsoCode = defaultIsoCode,
            IsoCodes = isoCodes
        };
    }

    private static DeepgramOptions CreateValidOptions(IReadOnlyDictionary<string, string> ttsModels)
    {
        return new DeepgramOptions
        {
            ApiKey = "k",
            Endpoint = "https://api.eu.deepgram.com/",
            TtsEndpoint = "https://api.eu.deepgram.com/",
            TtsModels = ttsModels
        };
    }

    private static IReadOnlyDictionary<string, string> CreateDefaultTtsModels()
    {
        return new Dictionary<string, string>
        {
            ["de"] = "aura-2-aurelia-de",
            ["en"] = "aura-2-helena-en"
        };
    }
}
