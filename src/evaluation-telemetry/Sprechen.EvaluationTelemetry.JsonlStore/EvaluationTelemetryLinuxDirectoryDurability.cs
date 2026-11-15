using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Sprechen.EvaluationTelemetry.JsonlStore;

/// <summary>
/// Synchronisiert Verzeichniseinträge unter Linux mit dem Datenträger,
/// um Dateiumbenennungen dauerhaft zu sichern.
/// </summary>
internal static partial class EvaluationTelemetryLinuxDirectoryDurability
{
    private const int OpenReadOnly = 0;
    private const int OpenDirectory = 0x10000;

    internal static void Flush(string directory)
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        var descriptor = Open(directory, OpenReadOnly | OpenDirectory);
        if (descriptor < 0)
        {
            throw new IOException(
                $"Could not open telemetry directory '{directory}' for a durability flush.",
                new Win32Exception(Marshal.GetLastPInvokeError()));
        }

        try
        {
            if (Fsync(descriptor) != 0)
            {
                throw new IOException(
                    $"Could not flush telemetry directory '{directory}' to disk.",
                    new Win32Exception(Marshal.GetLastPInvokeError()));
            }
        }
        finally
        {
            _ = Close(descriptor);
        }
    }

    [LibraryImport("libc", EntryPoint = "open", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int Open(string path, int flags);

    [LibraryImport("libc", EntryPoint = "fsync", SetLastError = true)]
    private static partial int Fsync(int descriptor);

    [LibraryImport("libc", EntryPoint = "close")]
    private static partial int Close(int descriptor);
}
