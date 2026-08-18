using System.Management;

namespace MechrevoCustomOSD;

internal sealed class FirmwareEvent
{
    internal required int Type { get; init; }
    internal required int Name { get; init; }
    internal required int Value { get; init; }
    internal required byte[] Raw { get; init; }

    public override string ToString()
    {
        return $"Type={Type}, Name={Name}, Value={Value}, Raw={BitConverter.ToString(Raw)}";
    }
}

internal sealed class FirmwareEventMonitor : IDisposable
{
    private readonly Action<FirmwareEvent> _callback;
    private ManagementEventWatcher? _watcher;

    internal FirmwareEventMonitor(Action<FirmwareEvent> callback)
    {
        _callback = callback;
    }

    internal void Start()
    {
        ConnectionOptions options = new() { EnablePrivileges = true };
        ManagementScope scope = new(@"\\.\root\WMI", options);
        scope.Connect();
        _watcher = new ManagementEventWatcher(scope, new WqlEventQuery("SELECT * FROM HID_EVENT20"));
        _watcher.EventArrived += EventArrived;
        _watcher.Start();
    }

    private void EventArrived(object sender, EventArrivedEventArgs eventArgs)
    {
        try
        {
            if (eventArgs.NewEvent["EventDetail"] is not byte[] detail || detail.Length < 3) return;

            int name = detail[1];
            int value = detail[2];
            if ((name == 26 || name == 32) && detail.Length >= 4)
            {
                value = (detail[2] << 8) + detail[3];
            }

            FirmwareEvent firmwareEvent = new()
            {
                Type = detail[0],
                Name = name,
                Value = value,
                Raw = detail
            };
            if (firmwareEvent.Type == 1)
            {
                _callback(firmwareEvent);
            }
            else
            {
                EventLogger.Write("Ignored non-hotkey firmware event: " + firmwareEvent);
            }
        }
        catch (Exception exception)
        {
            EventLogger.Write("Firmware event parse failure: " + exception);
        }
    }

    public void Dispose()
    {
        if (_watcher is null) return;
        try { _watcher.Stop(); } catch { }
        _watcher.EventArrived -= EventArrived;
        _watcher.Dispose();
        _watcher = null;
    }
}

internal static class EventLogger
{
    private static readonly object SyncRoot = new();
    private const long MaximumLogBytes = 2 * 1024 * 1024;

    internal static void Write(string message)
    {
        try
        {
            lock (SyncRoot)
            {
                AppDataPaths.EnsureDirectory();
                FileInfo log = new(AppDataPaths.LogPath);
                if (log.Exists && log.Length >= MaximumLogBytes)
                {
                    File.Move(AppDataPaths.LogPath, AppDataPaths.PreviousLogPath, true);
                }
                File.AppendAllText(AppDataPaths.LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}");
            }
        }
        catch
        {
        }
    }
}
