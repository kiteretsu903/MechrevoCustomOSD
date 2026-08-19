using Microsoft.UI.Dispatching;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace MechrevoCustomOSD;

internal sealed class TrayIcon : IDisposable
{
    private const uint WmNull = 0x0000;
    private const uint WmContextMenu = 0x007B;
    private const uint WmRButtonUp = 0x0205;
    private const uint NinSelect = 0x0400;
    private const uint NinKeySelect = 0x0401;
    private const uint WmApp = 0x8000;
    private const uint TrayCallbackMessage = WmApp + 42;
    private const uint RefreshTooltipMessage = WmApp + 43;
    private const uint ShutdownMessage = WmApp + 44;
    private const uint NimAdd = 0x00000000;
    private const uint NimModify = 0x00000001;
    private const uint NimDelete = 0x00000002;
    private const uint NimSetVersion = 0x00000004;
    private const uint NotifyIconVersion4 = 4;
    private const uint NifMessage = 0x00000001;
    private const uint NifIcon = 0x00000002;
    private const uint NifTip = 0x00000004;
    private const uint NifShowTip = 0x00000080;
    private const uint MfString = 0x00000000;
    private const uint MfGrayEd = 0x00000001;
    private const uint MfChecked = 0x00000008;
    private const uint MfPopup = 0x00000010;
    private const uint MfSeparator = 0x00000800;
    private const uint TpmRightButton = 0x0002;
    private const uint TpmNoNotify = 0x0080;
    private const uint TpmReturnCmd = 0x0100;
    private const uint ImageIcon = 1;
    private const uint LrShared = 0x00008000;
    private const int IdiApplication = 32512;
    private const uint CommandAuto = 1001;
    private const uint CommandChinese = 1002;
    private const uint CommandEnglish = 1003;
    private const uint CommandPreview = 1004;
    private const uint CommandExit = 1005;

    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Action _showPreview;
    private readonly Action _exit;
    private readonly WindowProcedure _windowProcedure;
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _ready = new(false);
    private readonly string _windowClassName;
    private Exception? _startupException;
    private nint _module;
    private nint _trayWindow;
    private nint _icon;
    private uint _taskbarCreatedMessage;
    private bool _classRegistered;
    private bool _ownsIcon;
    private bool _added;
    private bool _menuOpen;
    private bool _disposed;
    private NotifyIconData _data;

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        internal int X;
        internal int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Message
    {
        internal nint Window;
        internal uint Id;
        internal nuint WParam;
        internal nint LParam;
        internal uint Time;
        internal Point Cursor;
        internal uint Private;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WindowClass
    {
        internal uint Size;
        internal uint Style;
        internal nint WindowProcedure;
        internal int ClassExtra;
        internal int WindowExtra;
        internal nint Instance;
        internal nint Icon;
        internal nint Cursor;
        internal nint Background;
        internal string? MenuName;
        internal string ClassName;
        internal nint SmallIcon;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        internal uint Size;
        internal nint Window;
        internal uint Id;
        internal uint Flags;
        internal uint CallbackMessage;
        internal nint Icon;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        internal string Tip;

        internal uint State;
        internal uint StateMask;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        internal string Info;

        internal uint TimeoutOrVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        internal string InfoTitle;

        internal uint InfoFlags;
        internal Guid ItemGuid;
        internal nint BalloonIcon;
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate nint WindowProcedure(nint window, uint message, nint wParam, nint lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandleW(string? moduleName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassExW(ref WindowClass windowClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool UnregisterClassW(string className, nint instance);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(
        uint extendedStyle,
        string className,
        string windowName,
        uint style,
        int x,
        int y,
        int width,
        int height,
        nint parent,
        nint menu,
        nint instance,
        nint parameter);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(nint window);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint DefWindowProcW(nint window, uint message, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern int GetMessageW(out Message message, nint window, uint minimum, uint maximum);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref Message message);

    [DllImport("user32.dll")]
    private static extern nint DispatchMessageW(ref Message message);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int exitCode);

    [DllImport("user32.dll")]
    private static extern bool EndMenu();

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIconW(uint message, ref NotifyIconData data);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint ExtractIconExW(string file, int index, out nint largeIcon, out nint smallIcon, uint icons);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint LoadImageW(nint instance, nint name, uint type, int width, int height, uint flags);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(nint icon);

    [DllImport("user32.dll")]
    private static extern nint CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenuW(nint menu, uint flags, nuint item, string? text);

    [DllImport("user32.dll")]
    private static extern uint TrackPopupMenuEx(nint menu, uint flags, int x, int y, nint window, nint parameters);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(nint menu);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint window);

    [DllImport("user32.dll")]
    private static extern bool PostMessageW(nint window, uint message, nint wParam, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessageW(string message);

    private TrayIcon(DispatcherQueue dispatcherQueue, Action showPreview, Action exit)
    {
        _dispatcherQueue = dispatcherQueue;
        _showPreview = showPreview;
        _exit = exit;
        _windowProcedure = HandleWindowMessage;
        _windowClassName = "MechrevoCustomOSD.Tray." + Environment.ProcessId;
        _thread = new Thread(TrayThreadMain)
        {
            IsBackground = true,
            Name = "MechrevoCustomOSD.Tray"
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();

        if (!_ready.Wait(TimeSpan.FromSeconds(5)))
        {
            _disposed = true;
            throw new TimeoutException("Timed out while starting the tray icon thread.");
        }
        if (_startupException is not null)
        {
            _disposed = true;
            throw new InvalidOperationException("Unable to initialize the tray icon thread.", _startupException);
        }
    }

    internal static TrayIcon? TryCreate(
        DispatcherQueue dispatcherQueue,
        Action showPreview,
        Action exit)
    {
        try
        {
            return new TrayIcon(dispatcherQueue, showPreview, exit);
        }
        catch (Exception exception)
        {
            EventLogger.Write("Tray icon initialization failed: " + exception);
            return null;
        }
    }

    private void TrayThreadMain()
    {
        try
        {
            _module = GetModuleHandleW(null);
            _taskbarCreatedMessage = RegisterWindowMessageW("TaskbarCreated");

            WindowClass windowClass = new()
            {
                Size = (uint)Marshal.SizeOf<WindowClass>(),
                WindowProcedure = Marshal.GetFunctionPointerForDelegate(_windowProcedure),
                Instance = _module,
                ClassName = _windowClassName
            };
            if (RegisterClassExW(ref windowClass) == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to register the tray callback window.");
            }
            _classRegistered = true;

            _trayWindow = CreateWindowExW(
                0,
                _windowClassName,
                string.Empty,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                _module,
                0);
            if (_trayWindow == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to create the tray callback window.");
            }

            LoadApplicationIcon();
            AddIcon();
            _ready.Set();

            while (GetMessageW(out Message message, 0, 0, 0) > 0)
            {
                TranslateMessage(ref message);
                DispatchMessageW(ref message);
            }
        }
        catch (Exception exception)
        {
            _startupException = exception;
            EventLogger.Write("Tray icon thread failed: " + exception);
            _ready.Set();
        }
        finally
        {
            ReleaseNativeResources();
            if (_trayWindow != 0)
            {
                DestroyWindow(_trayWindow);
                _trayWindow = 0;
            }
            if (_classRegistered)
            {
                UnregisterClassW(_windowClassName, _module);
                _classRegistered = false;
            }
            _ready.Set();
        }
    }

    private nint HandleWindowMessage(nint window, uint message, nint wParam, nint lParam)
    {
        try
        {
            if (message == ShutdownMessage)
            {
                EndMenu();
                PostQuitMessage(0);
                return 0;
            }

            if (message == RefreshTooltipMessage)
            {
                RefreshTooltip();
                return 0;
            }

            if (message == _taskbarCreatedMessage)
            {
                _added = false;
                try { AddIcon(); } catch (Exception exception) { EventLogger.Write("Tray icon restore failed: " + exception.Message); }
                return 0;
            }

            if (message == TrayCallbackMessage)
            {
                uint notification = unchecked((uint)lParam.ToInt64()) & 0xFFFF;
                if (!_menuOpen && notification is WmContextMenu or WmRButtonUp or NinSelect or NinKeySelect)
                {
                    _menuOpen = true;
                    try
                    {
                        EventLogger.Write("Tray menu opened on dedicated thread.");
                        uint command = ShowMenu();
                        HandleCommand(command);
                    }
                    finally
                    {
                        _menuOpen = false;
                        EventLogger.Write("Tray menu closed.");
                    }
                }
                return 0;
            }
        }
        catch (Exception exception)
        {
            EventLogger.Write("Tray callback failure: " + exception);
        }

        return DefWindowProcW(window, message, wParam, lParam);
    }

    private void LoadApplicationIcon()
    {
        string executable = Environment.ProcessPath ?? string.Empty;
        if (!string.IsNullOrEmpty(executable) && ExtractIconExW(executable, 0, out nint large, out nint small, 1) > 0)
        {
            _icon = small != 0 ? small : large;
            _ownsIcon = _icon != 0;
            if (small != 0 && large != 0) DestroyIcon(large);
        }

        if (_icon == 0)
        {
            _icon = LoadImageW(0, IdiApplication, ImageIcon, 16, 16, LrShared);
            _ownsIcon = false;
        }
    }

    private void AddIcon()
    {
        _data = new NotifyIconData
        {
            Size = (uint)Marshal.SizeOf<NotifyIconData>(),
            Window = _trayWindow,
            Id = 1,
            Flags = NifMessage | NifIcon | NifTip | NifShowTip,
            CallbackMessage = TrayCallbackMessage,
            Icon = _icon,
            Tip = Localization.Text.TrayTooltip,
            Info = string.Empty,
            InfoTitle = string.Empty
        };

        _added = Shell_NotifyIconW(NimAdd, ref _data);
        if (!_added) throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to add tray icon.");

        _data.TimeoutOrVersion = NotifyIconVersion4;
        Shell_NotifyIconW(NimSetVersion, ref _data);
        EventLogger.Write("Tray icon added.");
    }

    private uint ShowMenu()
    {
        if (_disposed) return 0;

        LocalizedStrings text = Localization.Text;
        nint menu = CreatePopupMenu();
        nint languageMenu = CreatePopupMenu();
        if (menu == 0 || languageMenu == 0)
        {
            if (menu != 0) DestroyMenu(menu);
            if (languageMenu != 0) DestroyMenu(languageMenu);
            return 0;
        }

        try
        {
            AppendMenuW(menu, MfString | MfGrayEd, 0, text.TrayTooltip);
            AppendMenuW(menu, MfSeparator, 0, null);
            AppendLanguageItem(languageMenu, CommandAuto, text.TrayFollowSystem, LanguageMode.Auto);
            AppendLanguageItem(languageMenu, CommandChinese, text.TrayChinese, LanguageMode.Chinese);
            AppendLanguageItem(languageMenu, CommandEnglish, text.TrayEnglish, LanguageMode.English);
            AppendMenuW(menu, MfPopup, unchecked((nuint)languageMenu.ToInt64()), text.TrayLanguage);
            AppendMenuW(menu, MfString, CommandPreview, text.TrayPreview);
            AppendMenuW(menu, MfSeparator, 0, null);
            AppendMenuW(menu, MfString, CommandExit, text.TrayExit);

            GetCursorPos(out Point point);
            SetForegroundWindow(_trayWindow);
            uint command = TrackPopupMenuEx(
                menu,
                TpmRightButton | TpmNoNotify | TpmReturnCmd,
                point.X,
                point.Y,
                _trayWindow,
                0);
            PostMessageW(_trayWindow, WmNull, 0, 0);
            return command;
        }
        finally
        {
            DestroyMenu(menu);
        }
    }

    private static void AppendLanguageItem(nint menu, uint command, string label, LanguageMode mode)
    {
        uint flags = MfString;
        if (Localization.Mode == mode) flags |= MfChecked;
        AppendMenuW(menu, flags, command, label);
    }

    private void HandleCommand(uint command)
    {
        switch (command)
        {
            case CommandAuto:
                ChangeLanguage(LanguageMode.Auto);
                break;
            case CommandChinese:
                ChangeLanguage(LanguageMode.Chinese);
                break;
            case CommandEnglish:
                ChangeLanguage(LanguageMode.English);
                break;
            case CommandPreview:
                _dispatcherQueue.TryEnqueue(() => _showPreview());
                break;
            case CommandExit:
                _dispatcherQueue.TryEnqueue(() => _exit());
                break;
        }
    }

    private void ChangeLanguage(LanguageMode mode)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            Localization.SetMode(mode);
            nint trayWindow = _trayWindow;
            if (trayWindow != 0) PostMessageW(trayWindow, RefreshTooltipMessage, 0, 0);
            _showPreview();
        });
    }

    private void RefreshTooltip()
    {
        if (!_added) return;
        _data.Tip = Localization.Text.TrayTooltip;
        _data.Flags = NifTip;
        Shell_NotifyIconW(NimModify, ref _data);
        EventLogger.Write("Tray tooltip updated.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        nint trayWindow = _trayWindow;
        if (trayWindow != 0) PostMessageW(trayWindow, ShutdownMessage, 0, 0);
        bool stopped = Thread.CurrentThread == _thread || _thread.Join(TimeSpan.FromSeconds(3));
        if (!stopped)
        {
            EventLogger.Write("Tray icon thread did not stop within the timeout.");
        }
        else
        {
            _ready.Dispose();
        }
        EventLogger.Write("Tray icon removed.");
    }

    private void ReleaseNativeResources()
    {
        if (_added)
        {
            Shell_NotifyIconW(NimDelete, ref _data);
            _added = false;
        }

        if (_ownsIcon && _icon != 0) DestroyIcon(_icon);
        _icon = 0;
    }
}
