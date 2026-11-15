using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft den Feldgruppengraphen auf Zyklen und auf vom Einstieg
/// aus unerreichbare Feldgruppen.
/// </summary>
internal sealed class FormFieldGroupGraphValidator
{
    public void Validate(string entryFieldGroupId, FormContractPreflightIndex index, List<PreflightIssue> errors)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entryFieldGroupId);
        ArgumentNullException.ThrowIfNull(index);
        ArgumentNullException.ThrowIfNull(errors);
        ValidateFieldGroupAcyclic(index.FieldGroupsById, errors);
        ValidateEntryReachability(entryFieldGroupId, index.FieldGroupsById, errors);
    }
    private static void ValidateFieldGroupAcyclic(IReadOnlyDictionary<string, FieldGroupDefinition> fieldGroupsById, List<PreflightIssue> errors)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var stack = new HashSet<string>(StringComparer.Ordinal);

        foreach (var id in fieldGroupsById.Keys)
        {
            if (visited.Contains(id))
            {
                continue;
            }

            if (HasCycle(id))
            {
                break;
            }
        }

        bool HasCycle(string fieldGroupId)
        {
            if (stack.Contains(fieldGroupId))
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorFieldGroupCycle,
                    Message: $"Field-group cycle detected at '{fieldGroupId}'.",
                    Path: "$.fieldGroups[*].members",
                    Id: fieldGroupId));
                return true;
            }

            if (visited.Contains(fieldGroupId))
            {
                return false;
            }

            visited.Add(fieldGroupId);
            stack.Add(fieldGroupId);

            if (fieldGroupsById.TryGetValue(fieldGroupId, out var fieldGroup))
            {
                foreach (var member in fieldGroup.Members)
                {
                    if (!string.Equals(member.Kind, "fieldGroup", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(member.FieldGroupId))
                    {
                        continue;
                    }

                    if (HasCycle(member.FieldGroupId))
                    {
                        return true;
                    }
                }
            }

            stack.Remove(fieldGroupId);
            return false;
        }
    }

    private static void ValidateEntryReachability(string entryFieldGroupId, IReadOnlyDictionary<string, FieldGroupDefinition> fieldGroupsById, List<PreflightIssue> errors)
    {
        if (!fieldGroupsById.ContainsKey(entryFieldGroupId))
        {
            return;
        }

        var reachable = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>();
        reachable.Add(entryFieldGroupId);
        queue.Enqueue(entryFieldGroupId);

        while (queue.TryDequeue(out var current))
        {
            var fieldGroup = fieldGroupsById[current];
            foreach (var member in fieldGroup.Members)
            {
                if (!string.Equals(member.Kind, "fieldGroup", StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(member.FieldGroupId))
                {
                    continue;
                }

                if (reachable.Add(member.FieldGroupId) && fieldGroupsById.ContainsKey(member.FieldGroupId))
                {
                    queue.Enqueue(member.FieldGroupId);
                }
            }
        }

        foreach (var fieldGroupId in fieldGroupsById.Keys.OrderBy(id => id, StringComparer.Ordinal))
        {
            if (reachable.Contains(fieldGroupId))
            {
                continue;
            }

            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorFieldGroupUnreachable,
                Message: $"Field group '{fieldGroupId}' is not reachable from entryFieldGroupId '{entryFieldGroupId}'.",
                Path: "$.entryFieldGroupId",
                Id: fieldGroupId));
        }
    }

}
