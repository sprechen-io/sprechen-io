using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Hält Sprachcode und Höflichkeitsstufe unter Sperre und meldet
/// eine tatsächliche Änderung des normalisierten Sprachcodes an seine Abonnenten.
/// </summary>
public sealed class UserLanguageState : IUserLanguageState
{
    private readonly object _gate = new();
    private string? _selectedIsoCode;
    private PolitenessLevel? _selectedPolitenessLevel;

    public event Action<string>? SelectedLanguageChanged;

    public string? SelectedIsoCode
    {
        get
        {
            lock (_gate)
            {
                return _selectedIsoCode;
            }
        }
    }

    public bool HasSelection => !string.IsNullOrWhiteSpace(SelectedIsoCode);

    public PolitenessLevel? SelectedPolitenessLevel
    {
        get
        {
            lock (_gate)
            {
                return _selectedPolitenessLevel;
            }
        }
    }

    public void SetSelectedIsoCode(string isoCode)
    {
        var normalized = IsoCode.NormalizeIso6391(isoCode);

        lock (_gate)
        {
            if (string.Equals(_selectedIsoCode, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _selectedIsoCode = normalized;
        }

        SelectedLanguageChanged?.Invoke(normalized);
    }

    public void SetSelectedPolitenessLevel(PolitenessLevel? politenessLevel)
    {
        lock (_gate)
        {
            if (_selectedPolitenessLevel == politenessLevel)
            {
                return;
            }

            _selectedPolitenessLevel = politenessLevel;
        }
    }
}



