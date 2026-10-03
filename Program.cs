using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace WindowLayers;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, @"Local\WindowLayers_SingleInstance", out bool created);
        if (!created)
        {
            MessageBox.Show("窗口层级工具已经在运行。", "窗口层级工具");
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new SettingsForm());
    }
}

internal sealed class AppSettings
{
    public int TopKey { get; set; } = (int)Keys.T;
    public int TopModifiers { get; set; } = Native.MOD_CONTROL | Native.MOD_ALT;
    public int BottomKey { get; set; } = (int)Keys.B;
    public int BottomModifiers { get; set; } = Native.MOD_CONTROL | Native.MOD_ALT;
    public int Thickness { get; set; } = 5;
    public int Speed { get; set; } = 5;
    public int TopColor { get; set; } = Color.DeepSkyBlue.ToArgb();
    public int BottomColor { get; set; } = Color.OrangeRed.ToArgb();
    public bool Animate { get; set; } = true;
    public bool StartWithWindows { get; set; }

    public static string PathName => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WindowLayers", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(PathName)) ?? new AppSettings();
        }
        catch (IOException) { return new AppSettings(); }
        catch (JsonException) { return new AppSettings(); }
        catch (UnauthorizedAccessException) { return new AppSettings(); }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PathName)!);
        File.WriteAllText(PathName, JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }
}

internal static class Native
{
    public const int MOD_ALT = 0x0001;
    public const int MOD_CONTROL = 0x0002;
    public const int MOD_SHIFT = 0x0004;
    public const int MOD_WIN = 0x0008;
    public const int MOD_NOREPEAT = 0x4000;
    public const int WM_HOTKEY = 0x0312;
    public const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    public const int GW_HWNDNEXT = 2;
    public const int GW_HWNDPREV = 3;
    public const int SWP_NOSIZE = 0x0001;
    public const int SWP_NOMOVE = 0x0002;
    public const int SWP_NOACTIVATE = 0x0010;
    public const int SWP_NOOWNERZORDER = 0x0200;
    public const int WS_EX_TRANSPARENT = 0x00000020;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_TOPMOST = 0x00000008;
    public const int WS_EX_LAYERED = 0x00080000;
    public const int WS_EX_NOACTIVATE = 0x08000000;
    public static readonly IntPtr HWND_TOP = IntPtr.Zero;
    public static readonly IntPtr HWND_BOTTOM = new(1);
    public static readonly IntPtr HWND_TOPMOST = new(-1);
    public static readonly IntPtr HWND_NOTOPMOST = new(-2);
    public const int POSITION_FLAGS = SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER;
    public const int ULW_ALPHA = 0x00000002;

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X, Y;
        public POINT(int x, int y) { X = x; Y = y; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SIZE
    {
        public int Width, Height;
        public SIZE(int width, int height) { Width = width; Height = height; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BLENDFUNCTION
    {
        public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFOHEADER
    {
        public uint Size;
        public int Width, Height;
        public ushort Planes, BitCount;
        public uint Compression, SizeImage;
        public int XPelsPerMeter, YPelsPerMeter;
        public uint ClrUsed, ClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFO
    {
        public BITMAPINFOHEADER Header;
        public uint Colors;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
        public Rectangle ToRectangle() => Rectangle.FromLTRB(Left, Top, Right, Bottom);
    }

    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern IntPtr GetShellWindow();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(
        IntPtr hWnd, StringBuilder className, int capacity);
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr hWnd, int command);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] public static extern int GetWindowLong(IntPtr hWnd, int index);
    [DllImport("user32.dll", SetLastError = true)] public static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr after, int x, int y, int width, int height, int flags);
    [DllImport("user32.dll", SetLastError = true)] public static extern bool RegisterHotKey(
        IntPtr hWnd, int id, int modifiers, int key);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
    public delegate void WinEventCallback(IntPtr hook, uint eventType, IntPtr hWnd,
        int objectId, int childId, uint threadId, uint eventTime);
    [DllImport("user32.dll")] public static extern IntPtr SetWinEventHook(
        uint eventMin, uint eventMax, IntPtr module, WinEventCallback callback,
        uint processId, uint threadId, uint flags);
    [DllImport("user32.dll")] public static extern bool UnhookWinEvent(IntPtr hook);
    [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr hWnd, IntPtr dc);
    [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] public static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")] public static extern IntPtr CreateDIBSection(
        IntPtr dc, ref BITMAPINFO info, uint usage, out IntPtr bits, IntPtr section, uint offset);
    [DllImport("gdi32.dll")] public static extern IntPtr SelectObject(IntPtr dc, IntPtr bitmap);
    [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr handle);
    [DllImport("user32.dll", SetLastError = true)] public static extern bool UpdateLayeredWindow(
        IntPtr hWnd, IntPtr screenDc, ref POINT destination, ref SIZE size,
        IntPtr sourceDc, ref POINT source, int colorKey,
        ref BLENDFUNCTION blend, int flags);
}

internal enum BorderSide { Top, Right, Bottom, Left }

internal sealed class GlowStrip : Form
{
    private static readonly Color[] Rainbow = CreateRainbow();
    private readonly BorderSide _side;
    private IntPtr _memoryDc;
    private IntPtr _bitmap;
    private IntPtr _oldBitmap;
    private IntPtr _bits;
    private byte[] _pixels = Array.Empty<byte>();
    private Rectangle _lastBounds;
    private int _surfaceWidth;
    private int _surfaceHeight;

    public GlowStrip(BorderSide side)
    {
        _side = side;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= Native.WS_EX_TRANSPARENT | Native.WS_EX_TOOLWINDOW |
                Native.WS_EX_LAYERED | Native.WS_EX_NOACTIVATE;
            return parameters;
        }
    }

    public void Render(Rectangle bounds, int targetWidth, int targetHeight, int radius,
        int thickness, Color baseColor, double phase, bool animate, bool redraw)
    {
        bool resized = EnsureSurface(bounds.Width, bounds.Height);
        if (_memoryDc == IntPtr.Zero) return;
        if (!resized && !redraw && bounds == _lastBounds && Visible) return;
        if (resized || redraw) FillPixels(targetWidth, targetHeight, radius,
            thickness, baseColor, phase, animate);

        if (!Visible) Bounds = bounds;
        var destination = new Native.POINT(bounds.Left, bounds.Top);
        var size = new Native.SIZE(bounds.Width, bounds.Height);
        var source = new Native.POINT(0, 0);
        var blend = new Native.BLENDFUNCTION
        {
            BlendOp = 0,
            BlendFlags = 0,
            SourceConstantAlpha = 255,
            AlphaFormat = 1
        };
        var screenDc = Native.GetDC(IntPtr.Zero);
        bool updated = Native.UpdateLayeredWindow(Handle, screenDc, ref destination,
            ref size, _memoryDc, ref source, 0, ref blend, Native.ULW_ALPHA);
        Native.ReleaseDC(IntPtr.Zero, screenDc);
        if (updated)
        {
            _lastBounds = bounds;
            if (!Visible) Show();
        }
    }

    private bool EnsureSurface(int width, int height)
    {
        if (width == _surfaceWidth && height == _surfaceHeight && _memoryDc != IntPtr.Zero)
            return false;
        ReleaseSurface();
        if (width <= 0 || height <= 0) return false;
        var info = new Native.BITMAPINFO
        {
            Header = new Native.BITMAPINFOHEADER
            {
                Size = (uint)Marshal.SizeOf<Native.BITMAPINFOHEADER>(),
                Width = width,
                Height = -height,
                Planes = 1,
                BitCount = 32
            }
        };
        var screenDc = Native.GetDC(IntPtr.Zero);
        _memoryDc = Native.CreateCompatibleDC(screenDc);
        _bitmap = Native.CreateDIBSection(screenDc, ref info, 0, out _bits,
            IntPtr.Zero, 0);
        Native.ReleaseDC(IntPtr.Zero, screenDc);
        if (_memoryDc == IntPtr.Zero || _bitmap == IntPtr.Zero || _bits == IntPtr.Zero)
        {
            ReleaseSurface();
            return false;
        }
        _oldBitmap = Native.SelectObject(_memoryDc, _bitmap);
        _surfaceWidth = width;
        _surfaceHeight = height;
        _pixels = new byte[checked(width * height * 4)];
        return true;
    }

    private void FillPixels(int targetWidth, int targetHeight, int radius,
        int thickness, Color baseColor, double phase, bool animate)
    {
        int axisLength = _side is BorderSide.Top or BorderSide.Bottom
            ? _surfaceWidth : _surfaceHeight;
        int perimeter = 2 * (targetWidth + targetHeight);
        var colors = new Color[axisLength];
        var cornerFade = new double[axisLength];
        for (int axis = 0; axis < axisLength; axis++)
        {
            int outside = 0;
            int progress;
            if (_side is BorderSide.Top or BorderSide.Bottom)
            {
                int position = axis - radius;
                outside = position < 0 ? -position : Math.Max(0, position - targetWidth + 1);
                position = Math.Clamp(position, 0, targetWidth - 1);
                progress = _side == BorderSide.Top
                    ? position : targetWidth + targetHeight + targetWidth - 1 - position;
            }
            else
            {
                progress = _side == BorderSide.Right
                    ? targetWidth + axis
                    : 2 * targetWidth + targetHeight + targetHeight - 1 - axis;
            }
            double fade = Math.Clamp(1.0 - outside / (double)radius, 0, 1);
            cornerFade[axis] = fade * fade * (3 - 2 * fade);
            int hue = (int)(((baseColor.GetHue() / 360.0 + phase +
                progress / (double)perimeter) % 1.0) * Rainbow.Length);
            colors[axis] = animate ? Rainbow[hue] : baseColor;
        }

        for (int y = 0; y < _surfaceHeight; y++)
        {
            for (int x = 0; x < _surfaceWidth; x++)
            {
                int distance = _side switch
                {
                    BorderSide.Top => radius - 1 - y,
                    BorderSide.Bottom => y,
                    BorderSide.Left => radius - 1 - x,
                    _ => x
                };
                int axis = _side is BorderSide.Top or BorderSide.Bottom ? x : y;
                int alpha = (int)(FadeAlpha(distance, thickness, radius) * cornerFade[axis]);
                Color color = colors[axis];
                int offset = (y * _surfaceWidth + x) * 4;
                _pixels[offset] = (byte)(color.B * alpha / 255);
                _pixels[offset + 1] = (byte)(color.G * alpha / 255);
                _pixels[offset + 2] = (byte)(color.R * alpha / 255);
                _pixels[offset + 3] = (byte)alpha;
            }
        }
        Marshal.Copy(_pixels, 0, _bits, _pixels.Length);
    }

    private static int FadeAlpha(int distance, int thickness, int radius)
    {
        if (distance < thickness) return 230;
        double remaining = (radius - 1 - distance) / (double)(radius - thickness - 1);
        remaining = Math.Clamp(remaining, 0, 1);
        return (int)(230 * remaining * remaining * (3 - 2 * remaining));
    }

    private static Color[] CreateRainbow()
    {
        var colors = new Color[360];
        for (int degree = 0; degree < colors.Length; degree++)
        {
            double hue = degree / 60.0;
            int sector = (int)hue;
            double fraction = hue - sector;
            int low = 50;
            int mid = (int)(low + (255 - low) * fraction);
            int inverse = (int)(255 - (255 - low) * fraction);
            colors[degree] = sector switch
            {
                0 => Color.FromArgb(255, mid, low),
                1 => Color.FromArgb(inverse, 255, low),
                2 => Color.FromArgb(low, 255, mid),
                3 => Color.FromArgb(low, inverse, 255),
                4 => Color.FromArgb(mid, low, 255),
                _ => Color.FromArgb(255, low, inverse)
            };
        }
        return colors;
    }

    private void ReleaseSurface()
    {
        if (_memoryDc != IntPtr.Zero && _oldBitmap != IntPtr.Zero)
            Native.SelectObject(_memoryDc, _oldBitmap);
        if (_bitmap != IntPtr.Zero) Native.DeleteObject(_bitmap);
        if (_memoryDc != IntPtr.Zero) Native.DeleteDC(_memoryDc);
        _memoryDc = _bitmap = _oldBitmap = _bits = IntPtr.Zero;
        _surfaceWidth = _surfaceHeight = 0;
        _pixels = Array.Empty<byte>();
    }

    protected override void Dispose(bool disposing)
    {
        ReleaseSurface();
        base.Dispose(disposing);
    }
}

internal sealed class BorderForm : IDisposable
{
    private readonly bool _top;
    private readonly GlowStrip[] _strips =
    {
        new(BorderSide.Top), new(BorderSide.Right),
        new(BorderSide.Bottom), new(BorderSide.Left)
    };
    private Color _baseColor;
    private int _thickness;
    private int _speed;
    private bool _animate;
    private bool _dirty = true;
    private double _phase;

    public BorderForm(bool top)
    {
        _top = top;
        _baseColor = top ? Color.DeepSkyBlue : Color.OrangeRed;
    }

    public bool ContainsHandle(IntPtr handle) =>
        _strips.Any(strip => strip.IsHandleCreated && strip.Handle == handle);

    public void Configure(AppSettings settings)
    {
        _baseColor = Color.FromArgb(_top ? settings.TopColor : settings.BottomColor);
        _thickness = Math.Clamp(settings.Thickness, 2, 16);
        _speed = Math.Clamp(settings.Speed, 1, 10);
        _animate = settings.Animate;
        _dirty = true;
    }

    public void UpdateFor(IntPtr target)
    {
        if (!Native.IsWindow(target) || !Native.IsWindowVisible(target) ||
            Native.IsIconic(target) || !Native.GetWindowRect(target, out var rect))
        {
            Hide();
            return;
        }
        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;
        if (width < 20 || height < 20)
        {
            Hide();
            return;
        }
        int radius = _thickness + 24;
        if (_animate) _phase = (_phase + _speed * 0.025) % 1.0;
        var areas = new[]
        {
            new Rectangle(rect.Left - radius, rect.Top - radius, width + 2 * radius, radius),
            new Rectangle(rect.Right, rect.Top, radius, height),
            new Rectangle(rect.Left - radius, rect.Bottom, width + 2 * radius, radius),
            new Rectangle(rect.Left - radius, rect.Top, radius, height)
        };
        for (int i = 0; i < _strips.Length; i++)
            _strips[i].Render(areas[i], width, height, radius, _thickness,
                _baseColor, _phase, _animate, _animate || _dirty);
        _dirty = false;
        if (!IsStackedAbove(target)) PositionAbove(target);
    }

    private bool IsStackedAbove(IntPtr target)
    {
        var current = Native.GetWindow(target, Native.GW_HWNDPREV);
        for (int i = _strips.Length - 1; i >= 0; i--)
        {
            if (!_strips[i].Visible || current != _strips[i].Handle) return false;
            current = Native.GetWindow(current, Native.GW_HWNDPREV);
        }
        return true;
    }

    private void PositionAbove(IntPtr target)
    {
        var previous = Native.GetWindow(target, Native.GW_HWNDPREV);
        for (int i = 0; i < _strips.Length && ContainsHandle(previous); i++)
            previous = Native.GetWindow(previous, Native.GW_HWNDPREV);
        bool topmost = (Native.GetWindowLong(target, -20) & Native.WS_EX_TOPMOST) != 0;
        foreach (var strip in _strips)
        {
            if (!strip.Visible) continue;
            var after = previous == IntPtr.Zero
                ? (topmost ? Native.HWND_TOPMOST : Native.HWND_TOP)
                : previous;
            Native.SetWindowPos(strip.Handle, after, 0, 0, 0, 0, Native.POSITION_FLAGS);
            previous = strip.Handle;
        }
    }

    public void Hide()
    {
        foreach (var strip in _strips) strip.Hide();
    }

    public void Dispose()
    {
        foreach (var strip in _strips) strip.Dispose();
    }
}

internal sealed class SettingsForm : Form
{
    private AppSettings _settings = AppSettings.Load();
    private readonly BorderForm _topBorder = new(true);
    private readonly BorderForm _bottomBorder = new(false);
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 50 };
    private readonly Icon _appIcon;
    private readonly NotifyIcon _tray;
    private readonly ToolStripMenuItem _startupMenuItem;
    private readonly Native.WinEventCallback _foregroundCallback;
    private readonly TextBox _topHotkey = new();
    private readonly TextBox _bottomHotkey = new();
    private readonly Button _topColor = new();
    private readonly Button _bottomColor = new();
    private readonly NumericUpDown _thickness = new();
    private readonly NumericUpDown _speed = new();
    private readonly CheckBox _animate = new() { Text = "启用动态边框", AutoSize = true };
    private readonly Label _topStatus = new() { AutoSize = true };
    private readonly Label _bottomStatus = new() { AutoSize = true };
    private IntPtr _topWindow;
    private IntPtr _bottomWindow;
    private bool _exit;
    private int _tick;
    private int _editTopKey;
    private int _editTopMods;
    private int _editBottomKey;
    private int _editBottomMods;
    private IntPtr _foregroundHook;

    public SettingsForm()
    {
        _foregroundCallback = OnForegroundChanged;
        Text = "窗口层级工具";
        _appIcon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath)
            ?? SystemIcons.Application;
        Icon = _appIcon;
        Font = new Font("Microsoft YaHei UI", 9);
        AutoScaleMode = AutoScaleMode.Dpi;
        var workArea = Screen.FromPoint(Cursor.Position).WorkingArea;
        ClientSize = new Size(
            Math.Min(workArea.Width, Math.Min(700, Math.Max(420, workArea.Width - 80))),
            Math.Min(workArea.Height, Math.Min(820, Math.Max(400, workArea.Height - 80))));
        MinimumSize = new Size(Math.Min(570, workArea.Width), Math.Min(540, workArea.Height));
        StartPosition = FormStartPosition.Manual;
        Location = new Point(
            workArea.Left + (workArea.Width - Width) / 2,
            workArea.Top + (workArea.Height - Height) / 2);
        BackColor = Color.FromArgb(244, 247, 251);
        FormClosing += (_, e) =>
        {
            if (_exit) return;
            e.Cancel = true;
            Hide();
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("打开设置", null, (_, _) => ShowSettings());
        menu.Items.Add("取消置顶", null, (_, _) => ClearTop());
        menu.Items.Add("取消置底", null, (_, _) => ClearBottom(true));
        menu.Items.Add(new ToolStripSeparator());
        _startupMenuItem = new ToolStripMenuItem("开机自启动")
        {
            Checked = _settings.StartWithWindows
        };
        _startupMenuItem.Click += (_, _) => ToggleStartup();
        menu.Items.Add(_startupMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => Exit());
        _tray = new NotifyIcon
        {
            Text = "窗口层级工具",
            Icon = _appIcon,
            Visible = true,
            ContextMenuStrip = menu
        };
        _tray.DoubleClick += (_, _) => ShowSettings();

        BuildUi();
        LoadControls();
        _topBorder.Configure(_settings);
        _bottomBorder.Configure(_settings);
        Shown += (_, _) =>
        {
            PinSettings();
            if (!RegisterHotkeys(_settings))
                MessageBox.Show("默认快捷键注册失败，请在设置中改用其他组合。", Text);
            _foregroundHook = Native.SetWinEventHook(
                Native.EVENT_SYSTEM_FOREGROUND, Native.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero, _foregroundCallback, 0, 0, 0);
        };
        VisibleChanged += (_, _) =>
        {
            if (!Visible && _topWindow == Handle) ClearTop();
        };
        _timer.Tick += (_, _) => UpdateWindows();
        _timer.Start();
        UpdateStatus();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(24, 14, 24, 16),
            BackColor = Color.FromArgb(22, 41, 66)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        header.Controls.Add(new Label
        {
            Text = "窗口层级工具",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 16, FontStyle.Bold),
            ForeColor = Color.White,
            Margin = new Padding(0, 0, 0, 5)
        }, 0, 0);
        header.Controls.Add(new Label
        {
            Text = "配置快捷键与动态边框",
            AutoSize = true,
            ForeColor = Color.FromArgb(193, 210, 229),
            Margin = Padding.Empty
        }, 0, 1);
        root.Controls.Add(header, 0, 0);

        var viewport = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(22, 20, 22, 8),
            BackColor = BackColor
        };
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Padding = Padding.Empty,
            Margin = Padding.Empty
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        viewport.Controls.Add(content);
        root.Controls.Add(viewport, 0, 1);

        var shortcutRows = CreateRows(2);
        AddRow(shortcutRows, 0, "置顶快捷键", _topHotkey);
        AddRow(shortcutRows, 1, "置底快捷键", _bottomHotkey);
        _topHotkey.ReadOnly = true;
        _bottomHotkey.ReadOnly = true;
        StyleHotkeyBox(_topHotkey);
        StyleHotkeyBox(_bottomHotkey);
        _topHotkey.KeyDown += (_, e) => CaptureHotkey(e, true);
        _bottomHotkey.KeyDown += (_, e) => CaptureHotkey(e, false);
        content.Controls.Add(CreateCard("快捷键", "点击输入框，再按下包含修饰键的组合键。", shortcutRows));

        var effectRows = CreateRows(5);
        AddRow(effectRows, 0, "置顶边框颜色", _topColor);
        AddRow(effectRows, 1, "置底边框颜色", _bottomColor);
        _topColor.Text = "选择置顶颜色";
        _bottomColor.Text = "选择置底颜色";
        StyleColorButton(_topColor);
        StyleColorButton(_bottomColor);
        _topColor.Click += (_, _) => PickColor(true);
        _bottomColor.Click += (_, _) => PickColor(false);
        _thickness.Minimum = 2;
        _thickness.Maximum = 16;
        _speed.Minimum = 1;
        _speed.Maximum = 10;
        AddRow(effectRows, 2, "边框粗细", NumericField(_thickness, "像素"));
        AddRow(effectRows, 3, "动画速度", NumericField(_speed, "1 慢 · 10 快"));
        AddRow(effectRows, 4, "动态效果", _animate);
        var effectCard = CreateCard("边框效果", "颜色、粗细和速度会同时用于置顶与置底边框。", effectRows);
        effectCard.Margin = Padding.Empty;
        content.Controls.Add(effectCard);
        content.MinimumSize = new Size(
            Math.Max(510, (int)effectRows.ColumnStyles[0].Width +
                TextRenderer.MeasureText(_topColor.Text, _topColor.Font).Width + 84), 0);
        viewport.AutoScrollMinSize = new Size(content.MinimumSize.Width + viewport.Padding.Horizontal, 0);

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(22, 8, 22, 10),
            BackColor = Color.White
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(footer, 0, 2);
        var status = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = Padding.Empty
        };
        StyleStatus(_topStatus, Color.FromArgb(224, 247, 252));
        StyleStatus(_bottomStatus, Color.FromArgb(255, 237, 228));
        status.Controls.Add(_topStatus);
        status.Controls.Add(_bottomStatus);
        footer.Controls.Add(status, 0, 0);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = Padding.Empty
        };
        var apply = new Button { Text = "保存设置", AutoSize = true, MinimumSize = new Size(126, 40) };
        StyleActionButton(apply, true);
        apply.Click += (_, _) => SaveControls();
        actions.Controls.Add(apply);
        var hide = new Button { Text = "隐藏到托盘", AutoSize = true, MinimumSize = new Size(126, 40) };
        StyleActionButton(hide, false);
        hide.Click += (_, _) => Hide();
        actions.Controls.Add(hide);
        footer.Controls.Add(actions, 0, 1);
    }

    private TableLayoutPanel CreateRows(int count)
    {
        int labelWidth = Math.Max(155, TextRenderer.MeasureText("置顶边框颜色", Font).Width + 20);
        int rowHeight = Math.Max(46, TextRenderer.MeasureText("置顶边框颜色", Font).Height + 20);
        var rows = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = count,
            Margin = Padding.Empty
        };
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, labelWidth));
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < count; i++)
            rows.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));
        return rows;
    }

    private Control CreateCard(string title, string hint, TableLayoutPanel rows)
    {
        var card = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(18, 14, 18, 16),
            Margin = new Padding(0, 0, 0, 14),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        card.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            AutoSize = true,
            Font = new Font(Font.FontFamily, 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(27, 47, 72),
            Margin = new Padding(0, 0, 0, 5)
        }, 0, 0);
        card.Controls.Add(new Label
        {
            Text = hint,
            Dock = DockStyle.Fill,
            AutoSize = true,
            ForeColor = Color.FromArgb(100, 116, 139),
            Margin = new Padding(0, 0, 0, 10)
        }, 0, 1);
        card.Controls.Add(rows, 0, 2);
        return card;
    }

    private static void AddRow(TableLayoutPanel rows, int row, string title, Control input)
    {
        rows.Controls.Add(new Label
        {
            Text = title,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(46, 63, 84),
            Margin = Padding.Empty
        }, 0, row);
        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(0, 5, 0, 5);
        rows.Controls.Add(input, 1, row);
    }

    private static Control NumericField(NumericUpDown input, string unit)
    {
        var field = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty
        };
        input.Width = 105;
        input.Height = Math.Max(31, input.PreferredHeight);
        input.Margin = new Padding(0, 2, 12, 0);
        field.Controls.Add(input);
        field.Controls.Add(new Label
        {
            Text = unit,
            AutoSize = true,
            ForeColor = Color.FromArgb(100, 116, 139),
            Margin = new Padding(0, 7, 0, 0)
        });
        return field;
    }

    private static void StyleHotkeyBox(TextBox box)
    {
        box.BorderStyle = BorderStyle.FixedSingle;
        box.BackColor = Color.FromArgb(248, 250, 252);
        box.ForeColor = Color.FromArgb(24, 49, 75);
        box.Font = new Font(box.Font.FontFamily, 10, FontStyle.Bold);
    }

    private static void StyleColorButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Font = new Font(button.Font.FontFamily, 9, FontStyle.Bold);
    }

    private static void StyleStatus(Label label, Color background)
    {
        label.AutoSize = true;
        label.BackColor = background;
        label.ForeColor = Color.FromArgb(37, 62, 84);
        label.Padding = new Padding(10, 5, 10, 5);
        label.Margin = new Padding(0, 0, 10, 0);
    }

    private static void StyleActionButton(Button button, bool primary)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = Color.FromArgb(202, 213, 225);
        button.BackColor = primary ? Color.FromArgb(30, 111, 211) : Color.White;
        button.ForeColor = primary ? Color.White : Color.FromArgb(37, 62, 84);
        button.Font = new Font(button.Font.FontFamily, 9, FontStyle.Bold);
        button.Padding = new Padding(12, 6, 12, 6);
        button.Margin = new Padding(8, 0, 0, 0);
    }

    private void LoadControls()
    {
        _editTopKey = _settings.TopKey;
        _editTopMods = _settings.TopModifiers;
        _editBottomKey = _settings.BottomKey;
        _editBottomMods = _settings.BottomModifiers;
        _topHotkey.Text = FormatHotkey(_editTopKey, _editTopMods);
        _bottomHotkey.Text = FormatHotkey(_editBottomKey, _editBottomMods);
        SetColorButton(_topColor, Color.FromArgb(_settings.TopColor));
        SetColorButton(_bottomColor, Color.FromArgb(_settings.BottomColor));
        _thickness.Value = Math.Clamp(_settings.Thickness, 2, 16);
        _speed.Value = Math.Clamp(_settings.Speed, 1, 10);
        _animate.Checked = _settings.Animate;
    }

    private void CaptureHotkey(KeyEventArgs e, bool top)
    {
        e.SuppressKeyPress = true;
        var key = e.KeyCode;
        if (key is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin) return;
        if (key < Keys.A || key > Keys.Z)
        {
            MessageBox.Show("主键请使用 A 到 Z 的字母。", Text);
            return;
        }
        int modifiers = 0;
        if (e.Control) modifiers |= Native.MOD_CONTROL;
        if (e.Alt) modifiers |= Native.MOD_ALT;
        if (e.Shift) modifiers |= Native.MOD_SHIFT;
        if ((Native.GetAsyncKeyState((int)Keys.LWin) & 0x8000) != 0 ||
            (Native.GetAsyncKeyState((int)Keys.RWin) & 0x8000) != 0)
            modifiers |= Native.MOD_WIN;
        if (modifiers == 0)
        {
            MessageBox.Show("快捷键至少需要一个修饰键。", Text);
            return;
        }
        if (top)
        {
            _editTopKey = (int)key;
            _editTopMods = modifiers;
            _topHotkey.Text = FormatHotkey(_editTopKey, _editTopMods);
        }
        else
        {
            _editBottomKey = (int)key;
            _editBottomMods = modifiers;
            _bottomHotkey.Text = FormatHotkey(_editBottomKey, _editBottomMods);
        }
    }

    private static string FormatHotkey(int key, int modifiers)
    {
        var parts = new List<string>();
        if ((modifiers & Native.MOD_WIN) != 0) parts.Add("Win");
        if ((modifiers & Native.MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((modifiers & Native.MOD_ALT) != 0) parts.Add("Alt");
        if ((modifiers & Native.MOD_SHIFT) != 0) parts.Add("Shift");
        parts.Add(((Keys)key).ToString());
        return string.Join(" + ", parts);
    }

    private void PickColor(bool top)
    {
        using var dialog = new ColorDialog { FullOpen = true, Color = top ? _topColor.BackColor : _bottomColor.BackColor };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            if (top) SetColorButton(_topColor, dialog.Color);
            else SetColorButton(_bottomColor, dialog.Color);
        }
    }

    private static void SetColorButton(Button button, Color color)
    {
        button.BackColor = color;
        double brightness = color.R * 0.299 + color.G * 0.587 + color.B * 0.114;
        button.ForeColor = brightness < 145 ? Color.White : Color.FromArgb(19, 42, 65);
    }

    private void SaveControls()
    {
        if (_editTopKey == _editBottomKey && _editTopMods == _editBottomMods)
        {
            MessageBox.Show("置顶和置底不能使用同一快捷键。", Text);
            return;
        }
        var candidate = new AppSettings
        {
            TopKey = _editTopKey,
            TopModifiers = _editTopMods,
            BottomKey = _editBottomKey,
            BottomModifiers = _editBottomMods,
            TopColor = _topColor.BackColor.ToArgb(),
            BottomColor = _bottomColor.BackColor.ToArgb(),
            Thickness = (int)_thickness.Value,
            Speed = (int)_speed.Value,
            Animate = _animate.Checked,
            StartWithWindows = _settings.StartWithWindows
        };
        if (!RegisterHotkeys(candidate))
        {
            RegisterHotkeys(_settings);
            MessageBox.Show("快捷键被其他程序占用，请更换组合。", Text);
            return;
        }
        try
        {
            SetStartup(candidate.StartWithWindows);
            candidate.Save();
            _settings = candidate;
            _startupMenuItem.Checked = _settings.StartWithWindows;
            _topBorder.Configure(_settings);
            _bottomBorder.Configure(_settings);
            MessageBox.Show("设置已保存。", Text);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            RegisterHotkeys(_settings);
            try { SetStartup(_settings.StartWithWindows); } catch { }
            MessageBox.Show("保存失败：" + ex.Message, Text);
        }
    }

    private static void SetStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
        if (key is null) throw new IOException("无法打开启动项注册表。");
        if (enabled) key.SetValue("WindowLayers", "\"" + Application.ExecutablePath + "\"");
        else key.DeleteValue("WindowLayers", throwOnMissingValue: false);
    }

    private void ToggleStartup()
    {
        bool previous = _settings.StartWithWindows;
        try
        {
            SetStartup(!previous);
            _settings.StartWithWindows = !previous;
            _settings.Save();
            _startupMenuItem.Checked = !previous;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            _settings.StartWithWindows = previous;
            try { SetStartup(previous); } catch { }
            _startupMenuItem.Checked = previous;
            MessageBox.Show("更新开机自启动失败：" + ex.Message, Text);
        }
    }

    private bool RegisterHotkeys(AppSettings settings)
    {
        Native.UnregisterHotKey(Handle, 1);
        Native.UnregisterHotKey(Handle, 2);
        bool top = Native.RegisterHotKey(Handle, 1, settings.TopModifiers | Native.MOD_NOREPEAT, settings.TopKey);
        bool bottom = Native.RegisterHotKey(Handle, 2, settings.BottomModifiers | Native.MOD_NOREPEAT, settings.BottomKey);
        if (top && bottom) return true;
        Native.UnregisterHotKey(Handle, 1);
        Native.UnregisterHotKey(Handle, 2);
        return false;
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == Native.WM_HOTKEY)
        {
            if (message.WParam.ToInt32() == 1) ToggleTop();
            else if (message.WParam.ToInt32() == 2) ToggleBottom();
        }
        base.WndProc(ref message);
    }

    private IntPtr TargetWindow()
    {
        var target = Native.GetForegroundWindow();
        if (target == Handle || _topBorder.ContainsHandle(target) ||
            _bottomBorder.ContainsHandle(target) ||
            target == Native.GetShellWindow() || !Native.IsWindow(target) ||
            !Native.IsWindowVisible(target))
            return IntPtr.Zero;
        var className = new StringBuilder(64);
        Native.GetClassName(target, className, className.Capacity);
        if (className.ToString() is "Progman" or "WorkerW" or "Shell_TrayWnd")
            return IntPtr.Zero;
        return target;
    }

    private void ToggleTop()
    {
        if (Visible) return;
        var target = TargetWindow();
        if (target == IntPtr.Zero) return;
        if (target == _topWindow) { ClearTop(); return; }
        ClearTop();
        if (target == _bottomWindow) ClearBottom(false);
        if (!Native.SetWindowPos(target, Native.HWND_TOPMOST, 0, 0, 0, 0, Native.POSITION_FLAGS))
        {
            MessageBox.Show("无法置顶该窗口。管理员权限窗口可能需要以管理员身份运行本工具。", Text);
            return;
        }
        _topWindow = target;
        _topBorder.UpdateFor(target);
        UpdateStatus();
    }

    private void ToggleBottom()
    {
        var target = TargetWindow();
        if (target == IntPtr.Zero) return;
        if (target == _bottomWindow) { ClearBottom(true); return; }
        ClearBottom(true);
        if (target == _topWindow) ClearTop();
        if (!Native.SetWindowPos(target, Native.HWND_NOTOPMOST, 0, 0, 0, 0, Native.POSITION_FLAGS))
        {
            MessageBox.Show("无法置底该窗口。管理员权限窗口可能需要以管理员身份运行本工具。", Text);
            return;
        }
        _bottomWindow = target;
        if (!KeepBottom())
        {
            _bottomWindow = IntPtr.Zero;
            MessageBox.Show("无法置底该窗口。管理员权限窗口可能需要以管理员身份运行本工具。", Text);
            return;
        }
        _bottomBorder.UpdateFor(target);
        UpdateStatus();
    }

    private void ClearTop()
    {
        if (_topWindow == Handle)
            TopMost = false;
        else if (Native.IsWindow(_topWindow))
            Native.SetWindowPos(_topWindow, Native.HWND_NOTOPMOST, 0, 0, 0, 0, Native.POSITION_FLAGS);
        _topWindow = IntPtr.Zero;
        _topBorder.Hide();
        UpdateStatus();
    }

    private void ClearBottom(bool bringForward)
    {
        if (bringForward && Native.IsWindow(_bottomWindow))
            Native.SetWindowPos(_bottomWindow, Native.HWND_TOP, 0, 0, 0, 0, Native.POSITION_FLAGS);
        _bottomWindow = IntPtr.Zero;
        _bottomBorder.Hide();
        UpdateStatus();
    }

    private void UpdateWindows()
    {
        if (_topWindow != IntPtr.Zero && !Native.IsWindow(_topWindow)) ClearTop();
        if (_bottomWindow != IntPtr.Zero && !Native.IsWindow(_bottomWindow)) ClearBottom(false);
        if (_bottomWindow != IntPtr.Zero && ++_tick % 10 == 0)
            KeepBottom();
        if (_topWindow != IntPtr.Zero) _topBorder.UpdateFor(_topWindow);
        if (_bottomWindow != IntPtr.Zero) _bottomBorder.UpdateFor(_bottomWindow);
    }

    private bool KeepBottom()
    {
        if (_bottomWindow == IntPtr.Zero || !Native.IsWindow(_bottomWindow) ||
            Native.IsIconic(_bottomWindow)) return true;
        IntPtr lowestApplication = IntPtr.Zero;
        var current = Native.GetWindow(_bottomWindow, Native.GW_HWNDNEXT);
        for (int scanned = 0; current != IntPtr.Zero && scanned < 1024; scanned++)
        {
            if (IsApplicationWindow(current)) lowestApplication = current;
            current = Native.GetWindow(current, Native.GW_HWNDNEXT);
        }
        if (lowestApplication == IntPtr.Zero) return true;
        return Native.SetWindowPos(_bottomWindow, lowestApplication,
            0, 0, 0, 0, Native.POSITION_FLAGS);
    }

    private bool IsApplicationWindow(IntPtr handle)
    {
        if (handle == Handle || _topBorder.ContainsHandle(handle) ||
            _bottomBorder.ContainsHandle(handle) || handle == Native.GetShellWindow() ||
            !Native.IsWindowVisible(handle) || Native.IsIconic(handle) ||
            (Native.GetWindowLong(handle, -20) & Native.WS_EX_TOOLWINDOW) != 0)
            return false;
        var className = new StringBuilder(64);
        Native.GetClassName(handle, className, className.Capacity);
        return className.ToString() is not ("Progman" or "WorkerW" or "Shell_TrayWnd");
    }

    private void OnForegroundChanged(IntPtr hook, uint eventType, IntPtr hWnd,
        int objectId, int childId, uint threadId, uint eventTime)
    {
        if (hWnd != _bottomWindow || _bottomWindow == IntPtr.Zero || IsDisposed) return;
        BeginInvoke(new Action(() =>
        {
            if (hWnd == _bottomWindow) KeepBottom();
        }));
    }

    private void UpdateStatus()
    {
        _topStatus.Text = _topWindow == IntPtr.Zero ? "置顶：无" : "置顶：已启用";
        _bottomStatus.Text = _bottomWindow == IntPtr.Zero ? "置底：无" : "置底：已启用";
    }

    private void ShowSettings()
    {
        Show();
        WindowState = FormWindowState.Normal;
        PinSettings();
        Activate();
    }

    private void PinSettings()
    {
        if (_topWindow == Handle) return;
        ClearTop();
        TopMost = true;
        _topWindow = Handle;
        _topBorder.UpdateFor(Handle);
        UpdateStatus();
    }

    private void Exit()
    {
        _exit = true;
        _timer.Stop();
        ClearTop();
        ClearBottom(false);
        Native.UnregisterHotKey(Handle, 1);
        Native.UnregisterHotKey(Handle, 2);
        if (_foregroundHook != IntPtr.Zero) Native.UnhookWinEvent(_foregroundHook);
        _tray.Visible = false;
        _tray.Dispose();
        if (!ReferenceEquals(_appIcon, SystemIcons.Application)) _appIcon.Dispose();
        _topBorder.Dispose();
        _bottomBorder.Dispose();
        Close();
    }
}
