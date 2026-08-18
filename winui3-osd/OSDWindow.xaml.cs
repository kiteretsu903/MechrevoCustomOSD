using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices;
using Windows.UI;
using WinRT;

namespace MechrevoCustomOSD;

public sealed partial class OSDWindow : Window
{
    private const int GwlStyle = -16;
    private const int GwlExStyle = -20;
    private const long WsPopup = unchecked((long)0x80000000);
    private const long WsCaption = 0x00C00000;
    private const long WsThickFrame = 0x00040000;
    private const long WsSysMenu = 0x00080000;
    private const long WsMinimizeBox = 0x00020000;
    private const long WsMaximizeBox = 0x00010000;
    private const long WsExTopmost = 0x00000008;
    private const long WsExTransparent = 0x00000020;
    private const long WsExToolWindow = 0x00000080;
    private const long WsExAppWindow = 0x00040000;
    private const long WsExNoActivate = 0x08000000;
    private const int SwHide = 0;
    private const int SwShowNoActivate = 4;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;
    private const uint SwpShowWindow = 0x0040;
    private const uint MonitorDefaultToNearest = 2;
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwcpRound = 2;
    private const double LogicalWidth = 320;
    private const double LogicalHeight = 84;
    private static readonly nint HwndTopmost = new(-1);

    private readonly DispatcherQueue _dispatcherQueue;
    private readonly DispatcherQueueTimer _hideTimer;
    private readonly nint _hwnd;
    private DesktopAcrylicController? _acrylicController;
    private SystemBackdropConfiguration? _backdropConfiguration;
    private TrayIcon? _trayIcon;

    internal event Action? ExitRequested;

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        internal int X;
        internal int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        internal uint Size;
        internal Rect Monitor;
        internal Rect Work;
        internal uint Flags;
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint hWnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(nint hWnd, int index, nint value);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(nint hWnd, nint insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int command);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll")]
    private static extern nint MonitorFromPoint(Point point, uint flags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo monitorInfo);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint hWnd);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hWnd, int attribute, ref int value, int valueSize);

    public OSDWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _hideTimer = _dispatcherQueue.CreateTimer();
        _hideTimer.Interval = TimeSpan.FromMilliseconds(1850);
        _hideTimer.IsRepeating = false;
        _hideTimer.Tick += (_, _) => HideOverlay();

        _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        ConfigureNativeWindow();
        InitializeAcrylic();
        Activate();
        HideOverlay();
        _trayIcon = TrayIcon.TryCreate(
            _hwnd,
            _dispatcherQueue,
            ShowDemo,
            () => ExitRequested?.Invoke());
    }

    public new DispatcherQueue DispatcherQueue => _dispatcherQueue;

    private void InitializeAcrylic()
    {
        if (!DesktopAcrylicController.IsSupported())
        {
            EventLogger.Write("Desktop Acrylic is not supported; using fallback color.");
            return;
        }

        _backdropConfiguration = new SystemBackdropConfiguration
        {
            IsInputActive = true,
            IsHighContrast = false,
            Theme = SystemBackdropTheme.Dark
        };

        _acrylicController = new DesktopAcrylicController
        {
            Kind = DesktopAcrylicKind.Thin,
            TintColor = Color.FromArgb(255, 16, 18, 22),
            TintOpacity = 0.20f,
            LuminosityOpacity = 0.28f,
            FallbackColor = Color.FromArgb(255, 30, 32, 36)
        };

        ICompositionSupportsSystemBackdrop target = this.As<ICompositionSupportsSystemBackdrop>();
        if (!_acrylicController.AddSystemBackdropTarget(target))
        {
            _acrylicController.Dispose();
            _acrylicController = null;
            _backdropConfiguration = null;
            EventLogger.Write("Desktop Acrylic target registration failed; using fallback color.");
            return;
        }

        _acrylicController.SetSystemBackdropConfiguration(_backdropConfiguration);
        EventLogger.Write("Desktop Acrylic controller active with forced input-active state.");
    }

    internal void ShowDemo()
    {
        LocalizedStrings text = Localization.Text;
        ShowNotification(IconAsset.Performance, text.DemoTitle, text.DemoDetail, 72, Accent.Blue, 8000);
    }

    internal void ShowFirmwareEvent(FirmwareEvent firmwareEvent)
    {
        if (!_dispatcherQueue.HasThreadAccess)
        {
            _dispatcherQueue.TryEnqueue(() => ShowFirmwareEvent(firmwareEvent));
            return;
        }

        int value = firmwareEvent.Value;
        LocalizedStrings text = Localization.Text;
        switch (firmwareEvent.Name)
        {
            case 4:
                ShowNotification(IconAsset.Airplane, text.Airplane, value == 0 ? text.Disabled : text.Enabled, null, value == 0 ? Accent.Gray : Accent.Blue);
                break;
            case 5:
                ShowKeyboardBacklight(value, text);
                break;
            case 6:
                ShowNotification(IconAsset.Touchpad, text.Touchpad, value == 0 ? text.Disabled : text.Enabled, null, value == 0 ? Accent.Gray : Accent.Blue);
                break;
            case 7:
                ShowNotification(value == 0 ? IconAsset.Unlocked : IconAsset.Locked, text.FnLock, value == 0 ? text.Disabled : text.Enabled, null, value == 0 ? Accent.Gray : Accent.Blue);
                break;
            case 10:
                ShowNotification(IconAsset.Ambient, text.AmbientLight, value == 0 ? text.Disabled : text.Enabled, null, value == 0 ? Accent.Gray : Accent.Blue);
                break;
            case 15:
                ShowPerformanceMode(value, text);
                break;
            case 25:
                string refresh = value >= 30 ? value + " Hz" : string.Format(text.ModeFormat, value);
                ShowNotification(IconAsset.Refresh, text.RefreshRate, refresh, null, Accent.Purple);
                break;
            case 33:
                ShowNotification(value == 0 ? IconAsset.Unlocked : IconAsset.Locked, text.WindowsKeyLock, value == 0 ? text.Disabled : text.Enabled, null, value == 0 ? Accent.Gray : Accent.Blue);
                break;
        }
    }

    private void ShowKeyboardBacklight(int level, LocalizedStrings text)
    {
        string detail = level switch
        {
            0 => text.Disabled,
            1 => text.Low,
            2 => text.Medium,
            3 => text.High,
            128 => text.Automatic,
            _ => string.Format(text.LevelFormat, level)
        };

        double? progress = null;
        if (level is >= 0 and <= 3) progress = Math.Round(level * 100.0 / 3.0);
        else if (level is >= 0 and <= 10) progress = level * 10;
        ShowNotification(IconAsset.Keyboard, text.KeyboardBacklight, detail, progress, Accent.Purple);
    }

    private void ShowPerformanceMode(int mode, LocalizedStrings text)
    {
        switch (mode)
        {
            case 0:
                ShowNotification(IconAsset.Performance, text.PerformanceMode, text.HighPerformance, null, Accent.Red);
                break;
            case 1:
                ShowNotification(IconAsset.Balanced, text.PerformanceMode, text.Balanced, null, Accent.Blue);
                break;
            case 2:
                ShowNotification(IconAsset.Quiet, text.PerformanceMode, text.Quiet, null, Accent.Green);
                break;
            default:
                ShowNotification(IconAsset.Performance, text.PerformanceMode, string.Format(text.UnknownModeFormat, mode), null, Accent.Gray);
                break;
        }
    }

    private void ShowNotification(
        string iconFile,
        string title,
        string detail,
        double? progress,
        Color accent,
        int durationMilliseconds = 1850)
    {
        EventLogger.Write("Acrylic state at show: " + (_acrylicController?.State.ToString() ?? "Fallback"));
        string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "FluentEmoji3D", iconFile);
        IconImage.Source = new BitmapImage(new Uri(iconPath));
        TitleText.Text = title;
        DetailText.Text = detail;
        LevelBar.Foreground = new SolidColorBrush(accent);

        if (progress.HasValue)
        {
            LevelBar.Visibility = Visibility.Visible;
            LevelBar.Value = Math.Clamp(progress.Value, 0, 100);
        }
        else
        {
            LevelBar.Visibility = Visibility.Collapsed;
        }

        PositionAndShow();
        _hideTimer.Stop();
        _hideTimer.Interval = TimeSpan.FromMilliseconds(durationMilliseconds);
        _hideTimer.Start();
    }

    private void ConfigureNativeWindow()
    {
        long style = GetWindowLongPtr(_hwnd, GwlStyle).ToInt64();
        style &= ~(WsCaption | WsThickFrame | WsSysMenu | WsMinimizeBox | WsMaximizeBox);
        style |= WsPopup;
        SetWindowLongPtr(_hwnd, GwlStyle, new nint(style));

        long exStyle = GetWindowLongPtr(_hwnd, GwlExStyle).ToInt64();
        exStyle &= ~WsExAppWindow;
        exStyle |= WsExTopmost | WsExTransparent | WsExToolWindow | WsExNoActivate;
        SetWindowLongPtr(_hwnd, GwlExStyle, new nint(exStyle));

        int cornerPreference = DwmwcpRound;
        DwmSetWindowAttribute(_hwnd, DwmwaWindowCornerPreference, ref cornerPreference, sizeof(int));
    }

    private void PositionAndShow()
    {
        GetCursorPos(out Point cursor);
        nint monitor = MonitorFromPoint(cursor, MonitorDefaultToNearest);
        MonitorInfo monitorInfo = new() { Size = (uint)Marshal.SizeOf<MonitorInfo>() };
        GetMonitorInfo(monitor, ref monitorInfo);

        uint dpi = GetDpiForWindow(_hwnd);
        if (dpi == 0) dpi = 96;
        double scale = dpi / 96.0;
        int width = (int)Math.Round(LogicalWidth * scale);
        int height = (int)Math.Round(LogicalHeight * scale);
        int workWidth = monitorInfo.Work.Right - monitorInfo.Work.Left;
        int x = monitorInfo.Work.Left + ((workWidth - width) / 2);
        int y = monitorInfo.Work.Top + (int)Math.Round(32 * scale);

        SetWindowPos(_hwnd, HwndTopmost, x, y, width, height, SwpNoActivate | SwpFrameChanged | SwpShowWindow);
        ShowWindow(_hwnd, SwShowNoActivate);
    }

    private void HideOverlay()
    {
        ShowWindow(_hwnd, SwHide);
    }

    internal void CloseForShutdown()
    {
        _hideTimer.Stop();
        HideOverlay();
        _trayIcon?.Dispose();
        _trayIcon = null;
        _acrylicController?.RemoveAllSystemBackdropTargets();
        _acrylicController?.Dispose();
        _acrylicController = null;
        _backdropConfiguration = null;
        Close();
    }

    private static class Accent
    {
        internal static readonly Color Blue = Color.FromArgb(255, 74, 144, 226);
        internal static readonly Color Purple = Color.FromArgb(255, 162, 111, 255);
        internal static readonly Color Red = Color.FromArgb(255, 255, 91, 91);
        internal static readonly Color Green = Color.FromArgb(255, 72, 201, 126);
        internal static readonly Color Gray = Color.FromArgb(255, 137, 142, 153);
    }

    private static class IconAsset
    {
        internal const string Performance = "performance.png";
        internal const string Balanced = "balanced.png";
        internal const string Quiet = "quiet.png";
        internal const string Keyboard = "keyboard.png";
        internal const string Touchpad = "touchpad.png";
        internal const string Locked = "locked.png";
        internal const string Unlocked = "unlocked.png";
        internal const string Airplane = "airplane.png";
        internal const string Ambient = "ambient.png";
        internal const string Refresh = "refresh.png";
    }
}
