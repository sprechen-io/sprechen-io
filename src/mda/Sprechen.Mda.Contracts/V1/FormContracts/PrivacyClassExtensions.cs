namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Ordnet <see cref="PrivacyClass"/> den Übertragungswerten aus
/// <see cref="PrivacyClasses"/> zu, ohne deren Zeichenketten zu duplizieren.
/// </summary>
public static class PrivacyClassExtensions
{
    /// <summary>
    /// Liefert die zugehörige Übertragungskonstante. Unknown und andere nicht
    /// definierte Enumwerte lösen eine ArgumentOutOfRangeException aus.
    /// </summary>
    public static string ToWireToken(this PrivacyClass privacyClass)
    {
        return privacyClass switch
        {
            PrivacyClass.Public => PrivacyClasses.Public,
            PrivacyClass.Personal => PrivacyClasses.Personal,
            PrivacyClass.Sensitive => PrivacyClasses.Sensitive,
            PrivacyClass.Secret => PrivacyClasses.Secret,
            _ => throw new ArgumentOutOfRangeException(
                nameof(privacyClass), privacyClass, "Unknown privacy class has no wire token.")
        };
    }

    /// <summary>
    /// Ordnet einen exakt passenden Übertragungswert einer Datenschutzklasse zu.
    /// Bei null, Leerraum oder unbekannten Werten werden false und Unknown geliefert.
    /// </summary>
    public static bool TryFromWireToken(string? wire, out PrivacyClass privacyClass)
    {
        switch (wire)
        {
            case PrivacyClasses.Public:
                privacyClass = PrivacyClass.Public;
                return true;
            case PrivacyClasses.Personal:
                privacyClass = PrivacyClass.Personal;
                return true;
            case PrivacyClasses.Sensitive:
                privacyClass = PrivacyClass.Sensitive;
                return true;
            case PrivacyClasses.Secret:
                privacyClass = PrivacyClass.Secret;
                return true;
            default:
                privacyClass = PrivacyClass.Unknown;
                return false;
        }
    }
}
