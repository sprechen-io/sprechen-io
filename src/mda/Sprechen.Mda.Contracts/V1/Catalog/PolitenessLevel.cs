using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.Catalog;

/// <summary>
/// Legt die formelle oder informelle Anrede im Dialog fest.
/// </summary>
[JsonConverter(typeof(PolitenessLevelJsonConverter))]
public enum PolitenessLevel
{
    /// <summary>
    /// Formelle Anrede, beispielsweise die Sie-Form.
    /// </summary>
    Formal,

    /// <summary>
    /// Informelle Anrede, beispielsweise die Du-Form.
    /// </summary>
    Informal
}
