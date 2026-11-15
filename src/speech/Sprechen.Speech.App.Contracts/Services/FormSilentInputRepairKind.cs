namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Unterscheidet Korrekturbedarfe bei stiller Formulareingabe für passende Hinweise an der Oberfläche.
/// </summary>
public enum FormSilentInputRepairKind
{
    Required = 0,
    EmailFormat = 1,
    TelephoneFormat = 2,
    Length = 3,
    Format = 4,
}
