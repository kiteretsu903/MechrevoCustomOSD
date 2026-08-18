using Microsoft.UI.Xaml;
using System.Diagnostics;
using System.Threading;

namespace MechrevoCustomOSD;

public partial class App : Application
{
    private const string MutexName = @"Local\MechrevoCustomOSD";
    private const string StopEventName = @"Local\MechrevoCustomOSDStop";
    private const string DemoEventName = @"Local\MechrevoCustomOSDDemo";

    private Mutex? _mutex;
    private EventWaitHandle? _stopEvent;
    private EventWaitHandle? _demoEvent;
    private RegisteredWaitHandle? _stopWait;
    private RegisteredWaitHandle? _demoWait;
    private OSDWindow? _window;
    private FirmwareEventMonitor? _firmwareMonitor;
    private bool _shuttingDown;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Localization.Initialize();
        string[] commandLine = Environment.GetCommandLineArgs();
        if (HasArgument(commandLine, "--stop"))
        {
            SignalEvent(StopEventName);
            Exit();
            return;
        }

        bool demoRequested = HasArgument(commandLine, "--demo");
        if (demoRequested && SignalEvent(DemoEventName))
        {
            Exit();
            return;
        }

        _mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            Exit();
            return;
        }

        _stopEvent = new EventWaitHandle(false, EventResetMode.ManualReset, StopEventName);
        _demoEvent = new EventWaitHandle(false, EventResetMode.AutoReset, DemoEventName);
        _stopEvent.Reset();

        _window = new OSDWindow();
        _window.ExitRequested += Shutdown;
        _firmwareMonitor = new FirmwareEventMonitor(HandleFirmwareEvent);
        try
        {
            _firmwareMonitor.Start();
            EventLogger.Write("HID_EVENT20 watcher started.");
        }
        catch (Exception exception)
        {
            EventLogger.Write("HID_EVENT20 watcher failed: " + exception);
        }

        _stopWait = ThreadPool.RegisterWaitForSingleObject(
            _stopEvent,
            (_, _) => RequestShutdown(),
            null,
            Timeout.Infinite,
            true);
        _demoWait = ThreadPool.RegisterWaitForSingleObject(
            _demoEvent,
            (_, _) => RequestDemo(),
            null,
            Timeout.Infinite,
            false);

        EventLogger.Write("WinUI 3 custom OSD started in session " + Process.GetCurrentProcess().SessionId + ".");
        if (demoRequested)
        {
            _window.ShowDemo();
        }
    }

    private void HandleFirmwareEvent(FirmwareEvent firmwareEvent)
    {
        EventLogger.Write("Firmware event: " + firmwareEvent);
        _window?.ShowFirmwareEvent(firmwareEvent);
    }

    private void RequestDemo()
    {
        _window?.DispatcherQueue.TryEnqueue(() => _window.ShowDemo());
    }

    private void RequestShutdown()
    {
        _window?.DispatcherQueue.TryEnqueue(Shutdown);
    }

    private void Shutdown()
    {
        if (_shuttingDown) return;
        _shuttingDown = true;

        _stopWait?.Unregister(null);
        _demoWait?.Unregister(null);
        _firmwareMonitor?.Dispose();
        _window?.CloseForShutdown();
        _stopEvent?.Dispose();
        _demoEvent?.Dispose();

        if (_mutex is not null)
        {
            try { _mutex.ReleaseMutex(); } catch { }
            _mutex.Dispose();
        }

        EventLogger.Write("WinUI 3 custom OSD stopped.");
        Exit();
    }

    private static bool HasArgument(IEnumerable<string> arguments, string expected)
    {
        return arguments.Any(argument => string.Equals(argument, expected, StringComparison.OrdinalIgnoreCase));
    }

    private static bool SignalEvent(string name)
    {
        try
        {
            using EventWaitHandle handle = EventWaitHandle.OpenExisting(name);
            handle.Set();
            return true;
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
