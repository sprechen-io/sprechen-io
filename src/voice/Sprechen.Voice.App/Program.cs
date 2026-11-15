using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Sprechen.Voice.EvaluationTelemetry.BrowserInfrastructure;
using Sprechen.Web.SpeechClient;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddVoiceEvaluationTelemetryDelivery();
builder.Services.AddSprechenSpeechWebClient(builder.Configuration);

var host = builder.Build();
await host.Services.InitializeSprechenSpeechWebClientAsync(CancellationToken.None);
await host.RunAsync();
