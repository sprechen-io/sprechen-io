using Sprechen.ServiceDefaults;

namespace Sprechen.ServiceDefaults.Tests;

internal sealed class FakeApplicationVersionInfoProvider(ApplicationVersionInfo current) : IApplicationVersionInfoProvider
{
    private readonly ApplicationVersionInfo _current = current ?? throw new ArgumentNullException(nameof(current));

    public ApplicationVersionInfo GetCurrent() => _current;
}
