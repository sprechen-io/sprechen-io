using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Leitet Auswahloptionen aus Aufzählungswerten ab, damit die Oberfläche Wertcodes und zugehörige Beschriftungen gemeinsam erhält.
/// </summary>
internal sealed class FormEnumLikeOptionProjector
{
    private readonly FormSurfaceOptionLabelResolver _labelResolver;

    public FormEnumLikeOptionProjector()
        : this(new FormSurfaceOptionLabelResolver())
    {
    }

    internal FormEnumLikeOptionProjector(FormSurfaceOptionLabelResolver labelResolver)
    {
        _labelResolver = labelResolver ?? throw new ArgumentNullException(nameof(labelResolver));
    }

    public bool TryProject(
        FormContract contract,
        JsonElement valueType,
        string requestedLanguage,
        PolitenessLevel politenessLevel,
        out IReadOnlyList<FormSurfaceOption> options)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedLanguage);

        if (!Enum.IsDefined(politenessLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(politenessLevel), politenessLevel, "Unknown politeness level.");
        }

        options = Array.Empty<FormSurfaceOption>();

        if (valueType.ValueKind != JsonValueKind.Object
            || !valueType.TryGetProperty(ValueTypePropertyNames.Values, out var valuesElement)
            || valuesElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var parsed = new List<FormSurfaceOption>();
        foreach (var item in valuesElement.EnumerateArray())
        {
            if (TryProjectOption(contract, item, requestedLanguage, politenessLevel, out var option))
            {
                parsed.Add(option);
            }
        }

        if (parsed.Count == 0)
        {
            return false;
        }

        options = parsed;
        return true;
    }

    private bool TryProjectOption(
        FormContract contract,
        JsonElement item,
        string requestedLanguage,
        PolitenessLevel politenessLevel,
        out FormSurfaceOption option)
    {
        option = new FormSurfaceOption(string.Empty, string.Empty);

        if (item.ValueKind != JsonValueKind.Object
            || !item.TryGetProperty(ValueTypePropertyNames.Code, out var codeElement)
            || codeElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var code = codeElement.GetString();
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var trimmedCode = code.Trim();
        option = new FormSurfaceOption(trimmedCode, _labelResolver.Resolve(contract, item, trimmedCode, requestedLanguage, politenessLevel));
        return true;
    }
}
