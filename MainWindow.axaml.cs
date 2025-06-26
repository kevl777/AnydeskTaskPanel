using Avalonia.Controls;
using Avalonia.Input;
using System.Runtime.InteropServices;
using System;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;

namespace TestAvalonia;

public partial class MainWindow : Window
{
    private readonly System.Timers.Timer _mouseCheckTimer;
    private const string PositionFile = "window_position.txt";
    private NotifyIcon? _notifyIcon;

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }
    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT lpPoint);
    [DllImport("user32.dll")]
    public static extern bool ScreenToClient(nint hWnd, ref POINT lpPoint);

    public MainWindow()
    {
        InitializeComponent();
        LoadWindowPosition();
        ShowInTaskbar = false;
        var border = this.FindControl<Border>("border");
        border.PointerPressed += Border_PointerPressed;
        SetWindowExTopMost();

        _mouseCheckTimer = new System.Timers.Timer(50);
        _mouseCheckTimer.Elapsed += (s, e) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                var handleNullable = this.TryGetPlatformHandle()?.Handle;
                if (handleNullable is nint handle)
                {
                    POINT pt;
                    if (GetCursorPos(out pt))
                    {
                        var screenPt = pt;
                        ScreenToClient(handle, ref pt);
                        if (pt.X >= 0 && pt.X <= this.Bounds.Width && pt.Y >= 0 && pt.Y <= this.Bounds.Height)
                        {
                            this.Activate();
                        }
                    }
                }
            });
        };
        _mouseCheckTimer.Start();

        this.Closing += (_, __) => SaveWindowPosition();
        InitNotifyIcon();

        var inputBox = this.FindControl<Avalonia.Controls.TextBox>("InputBox");
        inputBox.KeyDown += InputBox_KeyDown;
        inputBox.GotFocus += (s, e) =>
        {
            inputBox.SelectAll();
        };

        this.PointerEntered += (_, __) =>
        {
            // ничего не делаем, окно становится активным по таймеру
        };
        this.PointerExited += (_, __) =>
        {
            this.Focus(); // убираем фокус с TextBox, окно становится неактивным
        };
    }

    private void Border_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void SetWindowExTopMost()
    {
        var handleNullable = this.TryGetPlatformHandle()?.Handle;
        if (handleNullable is nint handle)
        {
            const int GWL_EXSTYLE = -20;
            const int WS_EX_TOOLWINDOW = 0x00000080;
            const int WS_EX_TOPMOST = 0x00000008;
            int exStyle = GetWindowLong(handle, GWL_EXSTYLE);
            exStyle |= WS_EX_TOOLWINDOW | WS_EX_TOPMOST;
            SetWindowLong(handle, GWL_EXSTYLE, exStyle);
            SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, 0x0001 | 0x0002);
        }
    }

    private const int HWND_TOPMOST = -1;

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(nint hWnd, int nIndex);
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(nint hWnd, int hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    private void SaveWindowPosition()
    {
        try
        {
            File.WriteAllText(PositionFile, $"{Position.X},{Position.Y}");
        }
        catch { }
    }

    private void LoadWindowPosition()
    {
        try
        {
            if (File.Exists(PositionFile))
            {
                var text = File.ReadAllText(PositionFile);
                var parts = text.Split(',');
                if (parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
                {
                    Position = new PixelPoint(x, y);
                }
            }
        }
        catch { }
    }

    private void InitNotifyIcon()
    {
        _notifyIcon = new NotifyIcon();
        _notifyIcon.Icon = new Icon("icon.ico");
        _notifyIcon.Visible = true;
        _notifyIcon.Text = "AnyDeskWidget";
        _notifyIcon.Click += (s, e) =>
        {
            if (this.IsVisible)
                this.Hide();
            else
            {
                this.Show();
                this.Activate();
            }
        };
    }

    private void InputBox_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter)
        {
            var inputBox = sender as Avalonia.Controls.TextBox;
            var id = inputBox?.Text;
            if (!string.IsNullOrWhiteSpace(id))
            {
                try
                {
                    var anydeskPath = @"C:\\Program Files (x86)\\AnyDesk\\AnyDesk.exe";
                    Process.Start(new ProcessStartInfo(anydeskPath, id) { UseShellExecute = true });
                }
                catch { }
            }
            this.Focus(); // окно теряет фокус с TextBox, но остаётся видимым
        }
    }
}