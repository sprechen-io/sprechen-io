using System.Text.Json.Nodes;

namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Hält die Kennung einer geprüften Anwendungsdistribution und ihre Anwendungen zusammen, damit sie dem Katalog zugeordnet werden können.
/// </summary>
internal sealed record ApplicationDistributionFragment(string DistributionId, JsonArray Applications);
