namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Warnt vor Nicht-ASCII-Zeichen in Katalog-Tags, die das Suchverhalten beeinflussen können.
/// </summary>
internal sealed class CatalogTagPreflightValidator
{
    public void Validate(List<PreflightIssue> warnings, IReadOnlyList<string>? tags, string path, string id)
    {
        if (tags is null)
        {
            return;
        }

        for (var i = 0; i < tags.Count; i++)
        {
            var tag = tags[i];
            if (!IsAsciiOnly(tag))
            {
                warnings.Add(new PreflightIssue(
                    Code: PreflightCodes.WarnTagNonAscii,
                    Message: $"Tag '{tag}' contains non-ASCII characters, which may affect search behavior.",
                    Path: $"{path}[{i}]",
                    Id: id));
            }
        }
    }

    private static bool IsAsciiOnly(string text)
    {
        foreach (var character in text)
        {
            if (character > 127)
            {
                return false;
            }
        }

        return true;
    }
}
