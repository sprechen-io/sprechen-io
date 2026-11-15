using Sprechen.Forms.Pages.Models;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Forms.Pages;

/// <summary>
/// Projiziert die Formulare einer Anwendung zu geordneten Optionen (Id, lokalisierter Anzeigename, Vertragsversion, Sequenz).
/// </summary>
internal sealed class IndexPageFormOptionResolver
{
    public IReadOnlyList<FormOption> Resolve(ApplicationDescriptor application, string requestedLanguage)
    {
        ArgumentNullException.ThrowIfNull(application);

        return new FormsCatalogFormOrder().OrderForms(application)
            .Select(form =>
                new FormOption(
                    form.FormId.Value.Trim(),
                    new FormsCatalogTextResolver().ResolveFormDisplayName(application, form, requestedLanguage),
                    form.LatestContractVersion?.Trim() ?? string.Empty,
                    form.Sequence))
            .ToArray();
    }
}
