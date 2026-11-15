namespace Sprechen.Contracts.Services;

/// <summary>
/// Hält Artefaktdaten und zugehörige Modelle gemeinsam, damit Auslieferung und Prüfung auf denselben Bestand zugreifen können.
/// </summary>
internal sealed record ArtifactSet<TKey, TModel>(
    IReadOnlyDictionary<TKey, MdaJsonArtifact> Artifacts,
    IReadOnlyDictionary<TKey, TModel> Models)
    where TKey : notnull
    where TModel : class;

