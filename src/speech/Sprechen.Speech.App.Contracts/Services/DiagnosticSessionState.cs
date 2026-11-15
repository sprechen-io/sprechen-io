namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Diagnosesitzung zur Übergabe an die Browserkonsole. C# und TypeScript
/// verwenden damit dieselben Kennungen und denselben Betriebsmodus.
/// </summary>
public sealed record DiagnosticSessionState(
    bool Enabled,
    string? SessionId,
    string? ClientCorrelationId,
    string OperatingMode);
