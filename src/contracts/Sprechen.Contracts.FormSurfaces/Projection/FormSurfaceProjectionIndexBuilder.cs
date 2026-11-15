using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Erstellt Zuordnungen mit eindeutigen Kennungen, damit doppelt definierte Variablen, Felder oder Feldgruppen nicht unbemerkt überschrieben werden.
/// </summary>
internal sealed class FormSurfaceProjectionIndexBuilder
{
    public FormSurfaceProjectionIndex Build(FormContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        return new FormSurfaceProjectionIndex(
            IndexByExactId(contract.Variables, variable => variable.VariableId != default, variable => variable.VariableId.Value),
            IndexByExactId(contract.Fields, field => field.FieldId != default, field => field.FieldId.Value),
            IndexByExactId(contract.FieldGroups, fieldGroup => fieldGroup.FieldGroupId != default, fieldGroup => fieldGroup.FieldGroupId.Value));
    }

    private static Dictionary<string, T> IndexByExactId<T>(
        IEnumerable<T> items,
        Func<T, bool> hasIdentity,
        Func<T, string> keySelector)
    {
        var map = new Dictionary<string, T>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (item is null || !hasIdentity(item))
            {
                continue;
            }

            if (!map.TryAdd(keySelector(item), item))
            {
                throw new InvalidOperationException("A modeled identifier was not an exact unique canonical key.");
            }
        }

        return map;
    }
}
