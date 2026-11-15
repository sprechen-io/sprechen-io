namespace Sprechen.Speech.State.Tests.DialogHostExamples;

internal sealed class WizardFormsCatalog
{
    private readonly IReadOnlyDictionary<string, WizardApplicationDefinition> _apps;

    private WizardFormsCatalog(IReadOnlyDictionary<string, WizardApplicationDefinition> apps)
    {
        _apps = apps ?? throw new ArgumentNullException(nameof(apps));
    }

    public static WizardFormsCatalog CreateSample()
    {
        var shortForm = new WizardApplicationDefinition(
            Id: "short-form",
            Names: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["de"] = "Kurzformular",
                ["en"] = "Short form"
            },
            Fields: new[]
            {
                new WizardFieldDefinition(
                    WizardFieldId.LastName,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["de"] = "Nachname",
                        ["en"] = "Last name"
                    }),
                new WizardFieldDefinition(
                    WizardFieldId.Email,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["de"] = "E-Mail",
                        ["en"] = "Email"
                    })
            });

        var addressForm = new WizardApplicationDefinition(
            Id: "address-form",
            Names: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["de"] = "Adressformular",
                ["en"] = "Address form"
            },
            Fields: new[]
            {
                new WizardFieldDefinition(
                    WizardFieldId.Street,
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["de"] = "Straße",
                        ["en"] = "Street"
                    })
            });

        var apps = new Dictionary<string, WizardApplicationDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [shortForm.Id] = shortForm,
            [addressForm.Id] = addressForm
        };

        return new WizardFormsCatalog(apps);
    }

    public WizardApplicationDefinition GetById(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (!_apps.TryGetValue(id, out var app))
        {
            throw new InvalidOperationException($"Application '{id}' not found.");
        }

        return app;
    }

    public IReadOnlyList<WizardApplicationCandidate> Search(string languageCode, string query)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var results = new List<WizardApplicationCandidate>();

        foreach (var app in _apps.Values)
        {
            var name = GetLocalized(app.Names, languageCode) ?? app.Id;
            if (name.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new WizardApplicationCandidate(app.Id, name));
            }
        }

        return results;
    }

    private static string? GetLocalized(IReadOnlyDictionary<string, string> map, string languageCode)
    {
        if (map.TryGetValue(languageCode, out var value))
        {
            return value;
        }

        return map.TryGetValue("en", out var english) ? english : null;
    }
}


