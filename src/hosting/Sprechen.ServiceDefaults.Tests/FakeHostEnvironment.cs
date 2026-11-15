using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Sprechen.ServiceDefaults.Tests;

internal sealed class FakeHostEnvironment(string applicationName) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Production;

    public string ApplicationName { get; set; } = applicationName;

    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
