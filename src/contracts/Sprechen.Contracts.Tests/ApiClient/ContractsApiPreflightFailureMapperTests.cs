using Sprechen.Contracts.ApiClient;
using Sprechen.Mda.Contracts.V1.Preflight;
using Xunit;

namespace Sprechen.Contracts.Tests.ApiClient;

public sealed class ContractsApiPreflightFailureMapperTests
{
    [Fact]
    public void ThrowInvalidOperationIfFailed_WhenPreflightFails_PreservesInvalidOperationShape()
    {
        var mapper = new ContractsApiPreflightFailureMapper();
        var preflight = CreateFailedPreflight();

        var ex = Assert.Throws<InvalidOperationException>(
            () => mapper.ThrowInvalidOperationIfFailed(preflight, "FormsCatalog"));

        Assert.Contains("FormsCatalog preflight failed:", ex.Message, StringComparison.Ordinal);
        Assert.Contains(PreflightCodes.ErrorRefMissing, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThrowContractsPreflightFailedIfFailed_WhenPreflightFails_PreservesIssuePayload()
    {
        var mapper = new ContractsApiPreflightFailureMapper();
        var preflight = CreateFailedPreflight();

        var ex = Assert.Throws<ContractsPreflightFailedException>(
            () => mapper.ThrowContractsPreflightFailedIfFailed(preflight, "FormContract"));

        Assert.Contains("FormContract preflight failed:", ex.Message, StringComparison.Ordinal);
        Assert.Same(preflight.Errors, ex.Issues);
    }

    private static PreflightResult CreateFailedPreflight()
    {
        return new PreflightResult(
            [
                new PreflightIssue(
                    PreflightCodes.ErrorRefMissing,
                    "Missing reference.",
                    "$.applications[0]",
                    "sample")
            ]);
    }
}
