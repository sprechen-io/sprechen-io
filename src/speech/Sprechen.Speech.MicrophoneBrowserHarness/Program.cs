using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sprechen.Speech.App.CaptureRuntime.Services;
using Sprechen.Speech.App.MicrophoneRecording.Extensions;
using Sprechen.Speech.MicrophoneBrowserHarness;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<CaptureHarness>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddLogging();
builder.Services.AddMicrophoneRecording();
builder.Services.AddScoped(_ => new InMemorySpeechSampleRing(5_000));

await builder.Build().RunAsync();
