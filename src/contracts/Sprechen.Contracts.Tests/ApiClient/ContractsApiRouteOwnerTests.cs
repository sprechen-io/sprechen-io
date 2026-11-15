using Sprechen.Contracts.ApiClient;
using Xunit;

namespace Sprechen.Contracts.Tests.ApiClient;

public sealed class ContractsApiRouteOwnerTests
{
    [Fact]
    public void ResourceRoutes_EncodeContractSegments()
    {
        var routes = new ContractsApiResourceRoutes();

        var route = routes.FormContract("sample application", "form/1", "v 1");

        Assert.Equal("/contracts/sample%20application/form%2F1/v%201", route);
    }

    [Fact]
    public void ResourceRoutes_RejectBlankSegments()
    {
        var routes = new ContractsApiResourceRoutes();

        Assert.Throws<ArgumentException>(() => routes.FieldDefinitions(" ", "1"));
    }

    [Fact]
    public void ModelSchemaRoutes_ExposeContractSchemaPaths()
    {
        var routes = new ContractsApiModelSchemaRoutes();

        Assert.Equal("/contracts/schemas/forms-catalog", routes.FormsCatalogSchema());
        Assert.Equal("/contracts/schemas/form-contract", routes.FormContractSchema());
        Assert.Equal("/contracts/schemas/application-selection-dialog", routes.ApplicationSelectionDialogSchema());
    }

    [Fact]
    public void SharedSchemaRoutes_ExposeSharedSchemaPaths()
    {
        var routes = new ContractsApiSharedSchemaRoutes();

        Assert.Equal("/contracts/schemas/localization-primitives", routes.LocalizationPrimitivesSchema());
        Assert.Equal("/contracts/schemas/expression-language", routes.ExpressionLanguageSchema());
        Assert.Equal("/contracts/schemas/constraint-language", routes.ConstraintLanguageSchema());
        Assert.Equal("/contracts/schemas/privacy-policy", routes.PrivacyPolicySchema());
    }

    [Fact]
    public void ProxyRoute_NormalizesRelativeCatalogPaths()
    {
        var route = new ContractsApiProxyRoute();

        var path = route.CatalogProxyPath("/schemas/forms-catalog");

        Assert.Equal("contracts/schemas/forms-catalog", path);
    }

    [Fact]
    public void ProxyRoute_RejectsParentTraversal()
    {
        var route = new ContractsApiProxyRoute();

        Assert.Throws<ArgumentException>(() => route.CatalogProxyPath("../schemas/forms-catalog"));
    }
}
