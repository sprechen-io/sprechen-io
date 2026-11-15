using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Sucht zyklische Formularvoraussetzungen innerhalb einer Anwendung
/// und meldet den ersten gefundenen Zyklus.
/// </summary>
internal sealed class CatalogPrerequisiteCyclePreflightValidator
{
    public void Validate(List<PreflightIssue> errors, ApplicationDescriptor app, int appIndex)
    {
        var formPrereqs = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var form in app.Forms)
        {
            formPrereqs[form.FormId] = form.PrerequisiteForms?.ToList() ?? [];
        }

        foreach (var form in app.Forms)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var path = new List<string>();

            if (HasCycle(form.FormId, formPrereqs, visited, path))
            {
                path.Add(form.FormId);
                var cyclePath = string.Join(" -> ", path);

                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorPrerequisiteCycle,
                    Message: $"Cycle detected in prerequisiteForms: {cyclePath}",
                    Path: $"$.applications[{appIndex}].forms",
                    Id: form.FormId));

                break;
            }
        }
    }

    private static bool HasCycle(
        string formId,
        Dictionary<string, List<string>> formPrereqs,
        HashSet<string> visited,
        List<string> path)
    {
        if (!visited.Add(formId))
        {
            return true;
        }

        path.Add(formId);

        if (formPrereqs.TryGetValue(formId, out var prereqs) && HasCycleInPrerequisites(prereqs, formPrereqs, visited, path))
        {
            return true;
        }

        path.RemoveAt(path.Count - 1);
        visited.Remove(formId);

        return false;
    }

    private static bool HasCycleInPrerequisites(
        IEnumerable<string> prereqs,
        Dictionary<string, List<string>> formPrereqs,
        HashSet<string> visited,
        List<string> path)
    {
        foreach (var prereq in prereqs)
        {
            if (HasCycle(prereq, formPrereqs, visited, path))
            {
                return true;
            }
        }

        return false;
    }
}
