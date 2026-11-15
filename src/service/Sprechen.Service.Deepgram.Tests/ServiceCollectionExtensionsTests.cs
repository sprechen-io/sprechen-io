using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

using Sprechen.Service.Abstractions;
using Sprechen.Service.Deepgram.Services;
using Sprechen.Service.Core;

namespace Sprechen.Service.Deepgram.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDeepgramSpeech_WhenGlobalEndpointConfigured_ThrowsOnOptionsAccess()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Deepgram:ApiKey"] = "k",
                ["Deepgram:Endpoint"] = "https://api.deepgram.com/"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLanguageCore(config);
        services.AddDeepgramSpeech(config);

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<DeepgramOptions>>().Value);
    }

    [Fact]
    public void AddDeepgramSpeech_WhenGlobalTtsEndpointConfiguredWithExplicitOptOut_DoesNotThrowOnOptionsAccess()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Deepgram:ApiKey"] = "k",
                ["Deepgram:Endpoint"] = "https://api.eu.deepgram.com/",
                ["Deepgram:TtsEndpoint"] = "https://api.deepgram.com/",
                ["Deepgram:TtsAllowNonEuEndpoint"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLanguageCore(config);
        services.AddDeepgramSpeech(config);

        using var provider = services.BuildServiceProvider();

        _ = provider.GetRequiredService<IOptions<DeepgramOptions>>().Value;
    }

    [Fact]
    public void AddDeepgramSpeech_WhenGlobalTtsEndpointConfiguredWithoutOptOut_ThrowsOnOptionsAccess()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Deepgram:ApiKey"] = "k",
                ["Deepgram:Endpoint"] = "https://api.eu.deepgram.com/",
                ["Deepgram:TtsEndpoint"] = "https://api.deepgram.com/"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLanguageCore(config);
        services.AddDeepgramSpeech(config);

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<DeepgramOptions>>().Value);
    }

    [Fact]
    public void AddDeepgramSpeech_WhenEndpointContainsPath_ThrowsOnOptionsAccess()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Deepgram:ApiKey"] = "k",
                ["Deepgram:Endpoint"] = "https://api.eu.deepgram.com/v1/listen"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLanguageCore(config);
        services.AddDeepgramSpeech(config);

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<DeepgramOptions>>().Value);
    }

    [Fact]
    public void AddDeepgramSpeech_WhenProviderSelectedAndSampleRateIsNotNumeric_ThrowsOnOptionsAccess()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Language:SttProvider"] = "Deepgram",
                ["Language:TtsProvider"] = "Deepgram",
                ["Deepgram:ApiKey"] = "k",
                ["Deepgram:Endpoint"] = "https://api.eu.deepgram.com/",
                ["Deepgram:TtsEndpoint"] = "https://api.eu.deepgram.com/",
                ["Deepgram:TtsSampleRateHz"] = "not-a-number"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLanguageCore(config);
        services.AddDeepgramSpeech(config);

        using var provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IOptions<DeepgramOptions>>().Value);
    }

    [Fact]
    public void AddDeepgramSpeech_RegistersDeepgramAdapterAsConversationalOwner()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Language:SttProvider"] = "Deepgram",
                ["Language:TtsProvider"] = "Deepgram",
                ["SupportedLanguages:DefaultIsoCode"] = "de",
                ["SupportedLanguages:IsoCodes:0"] = "de",
                ["SupportedLanguages:IsoCodes:1"] = "en",
                ["Deepgram:ApiKey"] = "k",
                ["Deepgram:Endpoint"] = "https://api.eu.deepgram.com/",
                ["Deepgram:TtsEndpoint"] = "https://api.eu.deepgram.com/"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLanguageCore(config);
        services.AddDeepgramSpeech(config);

        using var provider = services.BuildServiceProvider();

        var resolved = provider.GetRequiredService<IConversationalSpeechToTextProvider>();
        var adapter = provider.GetRequiredService<DeepgramSpeechToTextAdapter>();

        Assert.IsType<DeepgramSpeechToTextAdapter>(resolved);
        Assert.Same(adapter, resolved);
    }

    [Fact]
    public void AddDeepgramSpeech_ResolvesSpeechToTextProviderToPrerecordedBackedAdapter()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Language:SttProvider"] = "Deepgram",
                ["Language:TtsProvider"] = "Deepgram",
                ["SupportedLanguages:DefaultIsoCode"] = "de",
                ["SupportedLanguages:IsoCodes:0"] = "de",
                ["SupportedLanguages:IsoCodes:1"] = "en",
                ["Deepgram:ApiKey"] = "k",
                ["Deepgram:Endpoint"] = "https://api.eu.deepgram.com/",
                ["Deepgram:TtsEndpoint"] = "https://api.eu.deepgram.com/"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLanguageCore(config);
        services.AddDeepgramSpeech(config);

        using var provider = services.BuildServiceProvider();

        var sttProvider = provider.GetRequiredService<ISpeechToTextProvider>();

        Assert.IsType<DeepgramSpeechToTextAdapter>(sttProvider);

        var prerecordedClient = provider.GetRequiredService<DeepgramPrerecordedTranscriptionClient>();

        Assert.NotNull(prerecordedClient);
        Assert.Same(prerecordedClient, provider.GetRequiredService<DeepgramPrerecordedTranscriptionClient>());
    }
}
