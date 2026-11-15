using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sprechen.MdaComposer;
using Sprechen.MdaComposer.SchemaEngine;
using Sprechen.MdaComposer.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.Configure<ComposerOptions>(
    builder.Configuration.GetSection(ComposerOptions.SectionName));
builder.Services.AddScoped(sp => new SchemaTextLoader(
    sp.GetRequiredService<HttpClient>(),
    sp.GetRequiredService<IOptions<ComposerOptions>>().Value));
builder.Services.AddScoped<ISchemaTextProvider>(sp => sp.GetRequiredService<SchemaTextLoader>());
builder.Services.AddScoped<ExampleAuthoringSession>();
builder.Services.AddScoped<CanonicalExportValidator>();
builder.Services.AddScoped<AuthoringValuePresetCatalog>();
builder.Services.AddScoped<ComposerJsInterop>();
builder.Services.AddScoped<SessionPersistenceStore>();
builder.Services.AddSingleton<ToastService>();

await builder.Build().RunAsync();
