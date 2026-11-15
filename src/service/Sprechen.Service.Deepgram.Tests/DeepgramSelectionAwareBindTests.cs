using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Sprechen.Service.Deepgram.Tests;

/// <summary>
/// Prüft die Registrierung der Deepgram-Startvalidierung abhängig von der konfigurierten Anbieterauswahl.
/// </summary>
public sealed class DeepgramSelectionAwareBindTests
{
    [Fact]
    public void AddDeepgramSpeech_WhenDeepgramSelected_WiresValidateOnStart()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Language:SttProvider"] = "Deepgram",
                ["Language:TtsProvider"] = "Deepgram",
                ["Deepgram:ApiKey"] = "k",
                ["Deepgram:Endpoint"] = "https://api.eu.deepgram.com/"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddDeepgramSpeech(config);

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IStartupValidator));
    }

    [Fact]
    public void AddDeepgramSpeech_WhenProviderSectionAbsent_WiresValidateOnStartByDefault()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Deepgram:ApiKey"] = "k",
                ["Deepgram:Endpoint"] = "https://api.eu.deepgram.com/"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddDeepgramSpeech(config);

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IStartupValidator));
    }

    [Fact]
    public void AddDeepgramSpeech_WhenNonDeepgramProviderSelected_DoesNotWireValidateOnStart()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Der synthetische Enumwert 99 prüft nur die Auswahlbedingung; er bezeichnet keinen implementierten Anbieter.
                ["Language:SttProvider"] = "99",
                ["Language:TtsProvider"] = "99",
                ["Deepgram:ApiKey"] = "k",
                ["Deepgram:Endpoint"] = "https://api.eu.deepgram.com/"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddDeepgramSpeech(config);

        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IStartupValidator));
    }
}
