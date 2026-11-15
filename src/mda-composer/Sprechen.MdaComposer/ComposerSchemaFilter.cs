using Sprechen.MdaComposer.SchemaEngine;

namespace Sprechen.MdaComposer;

/// <summary>
/// Filtert Schemas nach Anzeigename, Dateiname oder Modellbereich, damit die Auswahl durchsucht werden kann.
/// </summary>
internal sealed class ComposerSchemaFilter
{
    private readonly string? _filter;

    internal ComposerSchemaFilter(string? filter)
    {
        _filter = filter;
    }

    internal bool Matches(ComposerRootSchemaInfo schema)
    {
        if (string.IsNullOrWhiteSpace(_filter))
        {
            return true;
        }

        return schema.DisplayName.Contains(_filter, StringComparison.OrdinalIgnoreCase)
            || schema.FileName.Contains(_filter, StringComparison.OrdinalIgnoreCase)
            || schema.ModelArea.Contains(_filter, StringComparison.OrdinalIgnoreCase);
    }
}
