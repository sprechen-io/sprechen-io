using Sprechen.Contracts.FormSurfaces;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Forms.Services;

namespace Sprechen.Forms.Tests.TestDoubles;

internal sealed class DelegateReferenceFormFieldBuilder : IFormSurfaceDefinitionBuilder
{
    private readonly Func<FormContract, string, PolitenessLevel, FormSurfaceDefinition> _build;

    public DelegateReferenceFormFieldBuilder(Func<FormContract, string, PolitenessLevel, FormSurfaceDefinition> build)
    {
        _build = build ?? throw new ArgumentNullException(nameof(build));
    }

    public FormSurfaceDefinition BuildFormSurfaceDefinition(FormContract contract, string requestedLanguage, PolitenessLevel politenessLevel)
        => _build(contract, requestedLanguage, politenessLevel);
}


