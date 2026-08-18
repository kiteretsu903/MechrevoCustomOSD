using Microsoft.UI.Dispatching;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MechrevoCustomOSD;

internal sealed class TrayIcon : IDisposable
{
    private const int GwlWndProc = -4;
    private const uint TrayCallbackMessage = 0x8000 + 42;
    private const uint WmContextMenu = 0x007B;
    private const uint WmNull = 0x0000;
    private const uint WmRButtonUp = 0x0205;
    private const uint NinSelect = 0x0400;
    private const uint NinKeySelect = 0x0401;
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

    private readonly nint _window;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Action _showPreview;
    private readonly Action _exit;
    private readonly WindowProcedure _windowProcedure;
    private readonly uint _taskbarCreatedMessage;
    private nint _oldWindowProcedure;
    private nint _icon;
    private bool _ownsIcon;
    private bool _added;
    private bool _disposed;
    private NotifyIconData _data;

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        internal int X;
        internal int Y;
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

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint window, int index, nint newValue);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint CallWindowProc(nint previous, nint window, uint message, nint wParam, nint lParam);

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

    private TrayIcon(nint window, DispatcherQueue dispatcherQueue, Action showPreview, Action exit)
    {
        _window = window;
        _dispatcherQueue = dispatcherQueue;
        _showPreview = showPreview;
        _exit = exit;
        _windowProcedure = HandleWindowMessage;
        _taskbarCreatedMessage = RegisterWindowMessageW("TaskbarCreated");

        nint procedurePointer = Marshal.GetFunctionPointerForDelegate(_windowProcedure);
        Marshal.SetLastPInvokeError(0);
        _oldWindowProcedure = SetWindowLongPtr(_window, GwlWndProc, procedurePointer);
        int error = Marshal.GetLastWin32Error();
        if (_oldWindowProcedure == 0 && error != 0)
        {
            throw new Win32Exception(error, "Unable to install tray window callback.");
        }

        try
        {
            LoadApplicationIcon();
            AddIcon();
        }
        catch
        {
            ReleaseNativeResources();
            throw;
        }
    }

    internal static TrayIcon? TryCreate(
        nint window,
        DispatcherQueue dispatcherQueue,
        Action showPreview,
        Action exit)
    {
        try
        {
            return new TrayIcon(window, dispatcherQueue, showPreview, exit);
        }
        catch (Exception exception)
        {
            EventLogger.Write("Tray icon initialization failed: " + exception);
            return null;
        }
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
            Window = _window,
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

    private nint HandleWindowMessage(nint window, uint message, nint wParam, nint lParam)
    {
        if (message == _taskbarCreatedMessage)
        {
            _added = false;
            try { AddIcon(); } catch (Exception exception) { EventLogger.Write("Tray icon restore failed: " + exception.Message); }
            return 0;
        }

        if (message == TrayCallbackMessage)
        {
            uint notification = unchecked((uint)lParam.ToInt64()) & 0xFFFF;
            if (notification is WmContextMenu or WmRButtonUp or NinSelect or NinKeySelect)
            {
                _dispatcherQueue.TryEnqueue(ShowMenu);
                return 0;
            }
        }

        return _oldWindowProcedure == 0
            ? 0
            : CallWindowProc(_oldWindowProcedure, window, message, wParam, lParam);
    }

    private void ShowMenu()
    {
        if (_disposed) return;

        LocalizedStrings text = Localization.Text;
        nint menu = CreatePopupMenu();
        nint languageMenu = CreatePopupMenu();
        if (menu == 0 || languageMenu == 0)
        {
            if (menu != 0) DestroyMenu(menu);
            if (languageMenu != 0) DestroyMenu(languageMenu);
            return;
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
            SetForegroundWindow(_window);
            uint command = TrackPopupMenuEx(
                menu,
                TpmRightButton | TpmNoNotify | TpmReturnCmd,
                point.X,
                point.Y,
                _window,
                0);
            PostMessageW(_window, WmNull, 0, 0);
            HandleCommand(command);
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
                _showPreview();
                break;
            case CommandExit:
                _exit();
                break;
        }
    }

    private void ChangeLanguage(LanguageMode mode)
    {
        Localization.SetMode(mode);
        _data.Tip = Localization.Text.TrayTooltip;
        _data.Flags = NifTip;
        Shell_NotifyIconW(NimModify, ref _data);
        EventLogger.Write("Language mode changed to " + mode + ".");
        _showPreview();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        ReleaseNativeResources();
        EventLogger.Write("Tray icon removed.");
    }

    private void ReleaseNativeResources()
    {

        if (_added)
        {
            Shell_NotifyIconW(NimDelete, ref _data);
            _added = false;
        }

        if (_oldWindowProcedure != 0)
        {
            SetWindowLongPtr(_window, GwlWndProc, _oldWindowProcedure);
            _oldWindowProcedure = 0;
        }

        if (_ownsIcon && _icon != 0) DestroyIcon(_icon);
        _icon = 0;
    }
}
