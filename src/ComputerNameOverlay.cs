using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ComputerNameOverlay
{
    internal static class Program
    {
        internal static string StartupCommand = "\"" + Application.ExecutablePath + "\"";

        [STAThread]
        private static void Main(string[] args)
        {
            Native.SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length == 2 && args[0] == "--self-test")
            {
                try { SelfTest.Run(args[1]); }
                catch (Exception ex) { File.WriteAllText(args[1] + ".error.txt", ex.ToString()); Environment.ExitCode = 1; }
                return;
            }
            bool first;
            using (Mutex mutex = new Mutex(true, "Local\\ComputerNameOverlay-5F5A7C48", out first))
            {
                if (!first) return;
                try { using (OverlayContext context = new OverlayContext()) Application.Run(context); }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not start Computer Name Overlay.\n\n" + ex.Message,
                        "Computer Name Overlay", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally { mutex.ReleaseMutex(); }
            }
        }
    }

    internal sealed class OverlayContext : ApplicationContext
    {
        private readonly List<Watermark> windows = new List<Watermark>();
        private readonly NotifyIcon tray;
        private readonly ContextMenuStrip menu;
        private readonly System.Windows.Forms.Timer timer;
        private readonly ToolStripMenuItem startup;
        private readonly ToolStripMenuItem allScreens;
        private const string SettingsKey = @"Software\ComputerNameOverlay";
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private string layout = "";

        internal OverlayContext()
        {
            menu = new ContextMenuStrip();
            ToolStripMenuItem name = new ToolStripMenuItem("Computer: " + Environment.MachineName);
            name.Enabled = false;
            menu.Items.Add(name);
            menu.Items.Add(new ToolStripSeparator());
            allScreens = new ToolStripMenuItem("Show on all monitors");
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(SettingsKey))
                allScreens.Checked = key != null && Convert.ToString(key.GetValue("AllScreens", "0")) == "1";
            allScreens.Click += delegate
            {
                allScreens.Checked = !allScreens.Checked;
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(SettingsKey))
                        key.SetValue("AllScreens", allScreens.Checked ? "1" : "0");
                }
                catch (Exception ex) { ShowError(ex); }
                layout = "";
                RefreshWindows();
            };
            menu.Items.Add(allScreens);
            startup = new ToolStripMenuItem("Start when I sign in to Windows");
            startup.Checked = IsStartupEnabled();
            startup.Click += delegate
            {
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey))
                    {
                        if (IsStartupEnabled()) key.DeleteValue("ComputerNameOverlay", false);
                        else key.SetValue("ComputerNameOverlay", Program.StartupCommand);
                    }
                    startup.Checked = IsStartupEnabled();
                }
                catch (Exception ex) { ShowError(ex); }
            };
            menu.Items.Add(startup);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, delegate { ExitThread(); });
            tray = new NotifyIcon();
            tray.Icon = SystemIcons.Information;
            tray.Text = "Computer name: " + Environment.MachineName;
            tray.ContextMenuStrip = menu;
            tray.Visible = true;
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 2000;
            timer.Tick += delegate { RefreshWindows(); };
            RefreshWindows();
            timer.Start();
        }

        private static bool IsStartupEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey))
                return key != null && String.Equals(Convert.ToString(key.GetValue("ComputerNameOverlay")),
                    Program.StartupCommand, StringComparison.OrdinalIgnoreCase);
        }

        private static void ShowError(Exception ex)
        {
            MessageBox.Show("Could not save this setting.\n\n" + ex.Message, "Computer Name Overlay",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void RefreshWindows()
        {
            Screen[] screens = allScreens.Checked ? Screen.AllScreens : new Screen[] { Screen.PrimaryScreen };
            string signature = Environment.MachineName;
            foreach (Screen screen in screens)
                signature += "|" + screen.DeviceName + screen.WorkingArea.ToString() + Native.ScaleFor(screen).ToString();
            if (signature != layout)
            {
                foreach (Watermark window in windows) window.Dispose();
                windows.Clear();
                foreach (Screen screen in screens)
                {
                    Watermark window = new Watermark(screen.WorkingArea, Native.ScaleFor(screen));
                    windows.Add(window);
                    window.Show();
                    window.Render(Environment.MachineName);
                }
                layout = signature;
            }
            foreach (Watermark window in windows) window.KeepOnTop();
        }

        protected override void ExitThreadCore()
        {
            timer.Stop();
            tray.Visible = false;
            base.ExitThreadCore();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Dispose();
                tray.Dispose();
                menu.Dispose();
                foreach (Watermark window in windows) window.Dispose();
                windows.Clear();
            }
            base.Dispose(disposing);
        }
    }

    internal sealed class Watermark : Form
    {
        private readonly Rectangle area;
        private readonly float scale;
        internal Watermark(Rectangle workingArea, float dpiScale)
        {
            area = workingArea;
            scale = dpiScale;
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Bounds = new Rectangle(area.Right - 2, area.Bottom - 2, 1, 1);
            Text = "ComputerNameOverlay";
        }

        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= Native.WS_EX_LAYERED | Native.WS_EX_TRANSPARENT | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE;
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0084) { m.Result = new IntPtr(-1); return; } // HTTRANSPARENT
            if (m.Msg == 0x0021) { m.Result = new IntPtr(3); return; } // MA_NOACTIVATE
            base.WndProc(ref m);
        }

        internal void Render(string computerName)
        {
            using (Bitmap bitmap = CreateBitmap(computerName, scale, Math.Max(1, area.Width - (int)(56 * scale))))
            {
                int x = Math.Max(area.Left, area.Right - bitmap.Width - (int)(28 * scale));
                int y = Math.Max(area.Top, area.Bottom - bitmap.Height - (int)(38 * scale));
                Native.UpdateBitmap(Handle, bitmap, x, y);
            }
        }

        internal void KeepOnTop()
        {
            Native.SetWindowPos(Handle, new IntPtr(-1), 0, 0, 0, 0, 0x0013 | 0x0040);
        }

        internal static Bitmap CreateBitmap(string text, float scale, int maxWidth)
        {
            using (GraphicsPath path = new GraphicsPath())
            using (FontFamily family = new FontFamily("Segoe UI"))
            using (StringFormat format = new StringFormat(StringFormat.GenericTypographic))
            {
                path.AddString(text, family, (int)FontStyle.Regular, 21f * scale, PointF.Empty, format);
                RectangleF bounds = path.GetBounds();
                float padding = 5f * scale;
                float fit = Math.Min(1f, Math.Max(0.01f, (maxWidth - padding * 2) / Math.Max(1, bounds.Width)));
                using (Matrix transform = new Matrix(fit, 0, 0, fit, padding - bounds.X * fit, padding - bounds.Y * fit))
                    path.Transform(transform);
                Bitmap bitmap = new Bitmap(Math.Max(1, (int)Math.Ceiling(bounds.Width * fit + padding * 2)),
                    Math.Max(1, (int)Math.Ceiling(bounds.Height * fit + padding * 2)), PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bitmap))
                using (Pen outline = new Pen(Color.FromArgb(135, 0, 0, 0), 2.2f * scale))
                using (Brush fill = new SolidBrush(Color.FromArgb(185, 255, 255, 255)))
                {
                    g.Clear(Color.Transparent);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    outline.LineJoin = LineJoin.Round;
                    g.DrawPath(outline, path);
                    g.FillPath(fill, path);
                }
                return bitmap;
            }
        }
    }

    internal static class Native
    {
        internal const int WS_EX_LAYERED = 0x80000, WS_EX_TRANSPARENT = 0x20,
            WS_EX_TOOLWINDOW = 0x80, WS_EX_NOACTIVATE = 0x8000000;
        [DllImport("user32.dll")] internal static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] internal static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr dstDc,
            ref Point dst, ref Size size, IntPtr srcDc, ref Point src, int colorKey, ref Blend blend, int flags);
        [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(Point point, uint flags);
        [DllImport("shcore.dll")] private static extern int GetDpiForMonitor(IntPtr monitor, int type, out uint x, out uint y);
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Blend { public byte Operation, Flags, Alpha, Format; }

        internal static float ScaleFor(Screen screen)
        {
            uint x, y;
            IntPtr monitor = MonitorFromPoint(new Point(screen.Bounds.Left + 1, screen.Bounds.Top + 1), 2);
            return GetDpiForMonitor(monitor, 0, out x, out y) == 0 ? x / 96f : 1f;
        }

        internal static void UpdateBitmap(IntPtr hwnd, Bitmap bitmap, int x, int y)
        {
            IntPtr dc = GetDC(IntPtr.Zero);
            IntPtr memory = CreateCompatibleDC(dc);
            IntPtr hbitmap = IntPtr.Zero, old = IntPtr.Zero;
            try
            {
                hbitmap = bitmap.GetHbitmap(Color.FromArgb(0));
                old = SelectObject(memory, hbitmap);
                Point dst = new Point(x, y), src = Point.Empty;
                Size size = bitmap.Size;
                Blend blend = new Blend { Operation = 0, Flags = 0, Alpha = 255, Format = 1 };
                if (!UpdateLayeredWindow(hwnd, dc, ref dst, ref size, memory, ref src, 0, ref blend, 2))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                if (old != IntPtr.Zero) SelectObject(memory, old);
                if (hbitmap != IntPtr.Zero) DeleteObject(hbitmap);
                DeleteDC(memory);
                ReleaseDC(IntPtr.Zero, dc);
            }
        }
    }

    internal static class SelfTest
    {
        internal static void Run(string reportPath)
        {
            using (Bitmap bitmap = Watermark.CreateBitmap("DESKTOP-TEST123", 1f, 1000))
            {
                bool transparent = false, translucent = false, light = false, dark = false;
                for (int y = 0; y < bitmap.Height; y++)
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        Color pixel = bitmap.GetPixel(x, y);
                        transparent |= pixel.A == 0;
                        translucent |= pixel.A > 0 && pixel.A < 255;
                        light |= pixel.A > 100 && pixel.R > 180;
                        dark |= pixel.A > 50 && pixel.R < 50;
                    }
                if (!(transparent && translucent && light && dark)) throw new Exception("Bitmap contrast/alpha test failed.");
            }
            using (Bitmap narrow = Watermark.CreateBitmap(new string('W', 63), 2f, 400))
                if (narrow.Width > 400) throw new Exception("Text fitting failed.");
            using (Watermark window = new Watermark(Screen.PrimaryScreen.WorkingArea, 1f))
            {
                window.Render("DESKTOP-TEST123");
                int style = Native.GetWindowLong(window.Handle, -20);
                int required = Native.WS_EX_LAYERED | Native.WS_EX_TRANSPARENT | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE;
                if ((style & required) != required) throw new Exception("Overlay window styles failed.");
                window.KeepOnTop();
                if ((Native.GetWindowLong(window.Handle, -20) & 8) == 0) throw new Exception("Topmost style failed.");
            }
            using (Bitmap preview = new Bitmap(720, 240))
            using (Graphics g = Graphics.FromImage(preview))
            using (Bitmap text = Watermark.CreateBitmap("DESKTOP-DEMO", 1.4f, 650))
            {
                g.Clear(Color.White);
                g.FillRectangle(Brushes.Black, 0, 120, 720, 120);
                g.DrawImageUnscaled(text, 700 - text.Width, 50);
                g.DrawImageUnscaled(text, 700 - text.Width, 170);
                preview.Save(Path.ChangeExtension(reportPath, ".png"), ImageFormat.Png);
            }
            File.WriteAllText(reportPath, "PASS: alpha, light/dark contrast, long-name fitting, click-through/no-activate/tool/layered styles, UpdateLayeredWindow, topmost.\r\n");
        }
    }
}
