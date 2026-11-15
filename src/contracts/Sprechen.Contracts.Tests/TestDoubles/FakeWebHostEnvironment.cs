using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Sprechen.Contracts.Tests.TestDoubles;

internal sealed class FakeWebHostEnvironment : IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = "Production";

    public string ApplicationName { get; set; } = string.Empty;

    public string WebRootPath { get; set; } = string.Empty;

    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

    public string ContentRootPath { get; set; } = string.Empty;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}


