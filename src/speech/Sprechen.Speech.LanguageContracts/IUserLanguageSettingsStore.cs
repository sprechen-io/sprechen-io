namespace Sprechen.Speech.LanguageContracts;

/// <summary>
/// Liest und speichert Spracheinstellungen sowie den sprachbezogenen Marker des Einstiegsangebots.
/// </summary>
public interface IUserLanguageSettingsStore
{
    ValueTask<UserLanguageSettings> GetAsync(CancellationToken cancellationToken);

    ValueTask SetAsync(UserLanguageSettings settings, CancellationToken cancellationToken);

    /// <summary>
    /// Speichert ausschließlich den Sprachmarker des gehörten Einstiegsangebots.
    /// </summary>
    /// <remarks>
    /// Die Zusammenführung erfolgt im Speicherzugang. Ein vorausgehendes Lesen über
    /// <see cref="GetAsync"/> und anschließendes Schreiben über <see cref="SetAsync"/>
    /// könnte bei einem Lesefehler die bereits gespeicherte Sprach- und Höflichkeitswahl verlieren.
    /// </remarks>
    ValueTask SetEntryOfferHeardAsync(string isoCode, CancellationToken cancellationToken);
}
