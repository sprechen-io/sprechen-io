namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>Verwaltet den Kontext und die Katalogbindung des aktuellen Testlaufs.</summary>
public interface IOperationalProbeSession
{
    bool IsRequested { get; }

    OperationalProbeContext? Context { get; }

    void Request();

    void AcceptContext(OperationalProbeContext context);

    void BindStudy(OperationalProbeContext context, string applicationId, string taskId);

    bool Matches(string probeId, string condition, string applicationId, string taskId);

    bool OwnsTaskStorageKey(string storageKey);

    void Reset();
}
