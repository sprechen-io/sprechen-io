using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Hält Katalogbytes und gelesenes Dokument zusammen, damit Auslieferung und Prüfung dieselbe Katalogfassung verwenden können.
/// </summary>
internal sealed record FormsCatalogReferenceArtifact(MdaJsonArtifact Artifact, FormsCatalogDocument Document);
