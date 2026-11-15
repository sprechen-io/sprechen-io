using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet das gesprochene Formularargument zuerst einer Listennummer und sonst dem ersten passenden
/// Namen zu, damit die Formularwahl dieselbe Reihenfolge wie die Ansage verwendet.
/// </summary>
internal sealed class FormArgumentFormResolver
{
    private readonly FormsCatalogTextResolver textResolver;

    public FormArgumentFormResolver()
        : this(new FormsCatalogTextResolver())
    {
    }

    public FormArgumentFormResolver(FormsCatalogTextResolver textResolver)
    {
        this.textResolver = textResolver;
    }

    public bool TryResolveFormByArgument(
        ApplicationDescriptor application,
        IReadOnlyList<FormDescriptor> orderedForms,
        string isoCode,
        string argument,
        out FormDescriptor? form,
        out string? resolvedName)
    {
        form = null;
        resolvedName = null;

        if (application is null || orderedForms is null || orderedForms.Count == 0)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(argument) || string.IsNullOrWhiteSpace(isoCode))
        {
            return false;
        }

        var trimmedArgument = argument.Trim();
        var trimmedIsoCode = isoCode.Trim();
        if (TryResolveFormByNumber(application, orderedForms, trimmedIsoCode, trimmedArgument, out form, out resolvedName))
        {
            return true;
        }

        return TryResolveFormByName(application, orderedForms, trimmedIsoCode, trimmedArgument, out form, out resolvedName);
    }

    private bool TryResolveFormByNumber(
        ApplicationDescriptor application,
        IReadOnlyList<FormDescriptor> orderedForms,
        string isoCode,
        string argument,
        out FormDescriptor? form,
        out string? resolvedName)
    {
        form = null;
        resolvedName = null;
        if (!int.TryParse(argument, out var number) || number < 1 || number > orderedForms.Count)
        {
            return false;
        }

        form = orderedForms[number - 1];
        resolvedName = textResolver.ResolveFormDisplayName(application, form, isoCode);
        return true;
    }

    private bool TryResolveFormByName(
        ApplicationDescriptor application,
        IReadOnlyList<FormDescriptor> orderedForms,
        string isoCode,
        string argument,
        out FormDescriptor? form,
        out string? resolvedName)
    {
        form = null;
        resolvedName = null;
        var normalizedArgument = TextNormalizer.NormalizeForCompactMatching(argument);
        if (normalizedArgument.Length == 0)
        {
            return false;
        }

        foreach (var candidate in orderedForms)
        {
            if (candidate is null)
            {
                continue;
            }

            var candidateName = textResolver.ResolveFormDisplayName(application, candidate, isoCode);
            var normalizedName = TextNormalizer.NormalizeForCompactMatching(candidateName);

            if (normalizedName.Contains(normalizedArgument, StringComparison.OrdinalIgnoreCase))
            {
                form = candidate;
                resolvedName = candidateName;
                return true;
            }
        }

        return false;
    }
}
