using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace AutoTyper
{
    /// <summary>
    /// Window selector with proper resource management and no memory leaks
    /// </summary>
    public class WindowSelectorForm : Form, IDisposable
    {
        public IntPtr SelectedWindowHandle { get; private set; }
        public string SelectedWindowTitle { get; private set; } = "";

        private Label lblInstruction = null!;
        private Timer? cursorTimer;
        private IntPtr mouseHookHandle = IntPtr.Zero;
        
        // CRITICAL: Keep delegate as GC-rooted field to prevent collection
        private readonly LowLevelMouseProc mouseHookCallback;
        private bool disposed = false;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const uint GA_ROOT = 2;

        public WindowSelectorForm()
        {
            // Initialize delegate BEFORE setting hook
            mouseHookCallback = HookCallback;
            
            InitializeComponent();
            
            // Set hook after UI is ready
            try
            {
                mouseHookHandle = SetWindowsHookEx(WH_MOUSE_LL, mouseHookCallback, GetModuleHandle(null), 0);
                
                if (mouseHookHandle == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException($"Failed to set mouse hook. Error code: {error}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize window selector: {ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.Cancel;
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Window Selector - Click on target window";
            this.Size = new Size(500, 180);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.TopMost = true;
            this.BackColor = Color.LightYellow;

            lblInstruction = new Label();
            lblInstruction.Text = "1. Move mouse over target window\n2. Click LEFT mouse button to select\n\nESC - Cancel";
            lblInstruction.Location = new Point(20, 20);
            lblInstruction.Size = new Size(450, 120);
            lblInstruction.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblInstruction.TextAlign = ContentAlignment.MiddleCenter;
            lblInstruction.BorderStyle = BorderStyle.FixedSingle;
            lblInstruction.BackColor = Color.White;
            this.Controls.Add(lblInstruction);

            cursorTimer = new Timer();
            cursorTimer.Interval = 100;
            cursorTimer.Tick += CursorTimer_Tick;
            cursorTimer.Start();

            this.KeyPreview = true;
            this.KeyDown += WindowSelectorForm_KeyDown;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_LBUTTONDOWN)
            {
                try
                {
                    NativeMethods.POINT cursorPos;
                    if (!NativeMethods.GetCursorPos(out cursorPos))
                        return CallNextHookEx(mouseHookHandle, nCode, wParam, lParam);

                    IntPtr hwnd = NativeMethods.WindowFromPoint(cursorPos);
                    if (hwnd == IntPtr.Zero)
                        return CallNextHookEx(mouseHookHandle, nCode, wParam, lParam);

                    // Get the root window (not child controls)
                    IntPtr rootHwnd = GetAncestor(hwnd, GA_ROOT);
                    
                    // Validate handles
                    if (rootHwnd == IntPtr.Zero)
                        rootHwnd = hwnd;

                    // Check if it's not this form or the main Auto Typer form
                    if (rootHwnd != this.Handle && rootHwnd != this.Owner?.Handle)
                    {
                        StringBuilder sb = new StringBuilder(256);
                        int length = NativeMethods.GetWindowText(rootHwnd, sb, sb.Capacity);
                        
                        if (length > 0)
                        {
                            string title = sb.ToString();
                            
                            // Don't select Auto Typer windows
                            if (!title.Contains("Auto Typer") && !title.Contains("Window Selector"))
                            {
                                SelectedWindowHandle = rootHwnd;
                                SelectedWindowTitle = title;

                                // Use BeginInvoke to avoid cross-thread issues
                                this.BeginInvoke(new Action(() =>
                                {
                                    try
                                    {
                                        if (cursorTimer != null)
                                        {
                                            cursorTimer.Stop();
                                            cursorTimer.Dispose();
                                            cursorTimer = null;
                                        }
                                        
                                        this.DialogResult = DialogResult.OK;
                                        this.Close();
                                    }
                                    catch (ObjectDisposedException)
                                    {
                                        // Form already disposed, ignore
                                    }
                                }));

                                return (IntPtr)1; // Block the click
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Silently handle errors in hook to prevent crashes
                }
            }
            
            return CallNextHookEx(mouseHookHandle, nCode, wParam, lParam);
        }

        private void CursorTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                NativeMethods.POINT cursorPos;
                if (!NativeMethods.GetCursorPos(out cursorPos))
                    return;

                IntPtr hwnd = NativeMethods.WindowFromPoint(cursorPos);
                if (hwnd == IntPtr.Zero)
                    return;

                StringBuilder sb = new StringBuilder(256);
                int length = NativeMethods.GetWindowText(hwnd, sb, sb.Capacity);

                if (length > 0)
                {
                    string title = sb.ToString();
                    lblInstruction.Text = $"Window under cursor:\n\"{title}\"\n\nClick LEFT mouse button | ESC to cancel";
                    lblInstruction.BackColor = Color.LightGreen;
                }
                else
                {
                    lblInstruction.Text = "1. Move mouse over target window\n2. Click LEFT mouse button to select\n\nESC - Cancel";
                    lblInstruction.BackColor = Color.White;
                }
            }
            catch (Exception)
            {
                // Silently handle errors to prevent crashes
            }
        }

        private void WindowSelectorForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            CleanupResources();
            base.OnFormClosing(e);
        }

        private void CleanupResources()
        {
            // Stop timer first
            if (cursorTimer != null)
            {
                cursorTimer.Stop();
                cursorTimer.Dispose();
                cursorTimer = null;
            }

            // Unhook mouse hook
            if (mouseHookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(mouseHookHandle);
                mouseHookHandle = IntPtr.Zero;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    CleanupResources();
                }
                disposed = true;
            }
            base.Dispose(disposing);
        }

        // Finalizer as safety net
        ~WindowSelectorForm()
        {
            Dispose(false);
        }
    }
}
