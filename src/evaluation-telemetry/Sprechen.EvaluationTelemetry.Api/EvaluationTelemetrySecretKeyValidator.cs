using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Sprechen.EvaluationTelemetry.Api;

/// <summary>
/// Vergleicht die SHA-256-Hashwerte des empfangenen und erwarteten Schlüssels
/// mit einem zeitkonstanten Vergleich.
/// </summary>
internal static class EvaluationTelemetrySecretKeyValidator
{
    public static bool HasValidKey(HttpRequest request, string headerName, string expectedKey)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(headerName);

        if (string.IsNullOrEmpty(expectedKey)
            || !request.Headers.TryGetValue(headerName, out var suppliedValues)
            || suppliedValues.Count != 1)
        {
            return false;
        }

        var suppliedKey = suppliedValues[0];
        if (string.IsNullOrEmpty(suppliedKey))
        {
            return false;
        }

        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expectedKey));
        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(suppliedKey));
        return CryptographicOperations.FixedTimeEquals(expectedHash, suppliedHash);
    }
}
