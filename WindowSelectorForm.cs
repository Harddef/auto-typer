using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace AutoTyper
{
    public class WindowSelectorForm : Form
    {
        public IntPtr SelectedWindowHandle { get; private set; }
        public string SelectedWindowTitle { get; private set; } = "";

        private Label lblInstruction = null!;
        private Timer cursorTimer = null!;
        private IntPtr mouseHookHandle = IntPtr.Zero;
        private LowLevelMouseProc mouseHookCallback;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;

        public WindowSelectorForm()
        {
            InitializeComponent();
            mouseHookCallback = HookCallback;
            mouseHookHandle = SetWindowsHookEx(WH_MOUSE_LL, mouseHookCallback, GetModuleHandle(null), 0);
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
                NativeMethods.POINT cursorPos;
                NativeMethods.GetCursorPos(out cursorPos);
                IntPtr hwnd = NativeMethods.WindowFromPoint(cursorPos);

                // Get the root window (not child controls)
                IntPtr rootHwnd = GetAncestor(hwnd, 2); // GA_ROOT = 2
                
                // Check if it's not this form or the main Auto Typer form
                if (hwnd != IntPtr.Zero && rootHwnd != this.Handle && rootHwnd != this.Owner?.Handle)
                {
                    StringBuilder sb = new StringBuilder(256);
                    NativeMethods.GetWindowText(rootHwnd, sb, sb.Capacity);
                    
                    // Don't select Auto Typer window
                    string title = sb.ToString();
                    if (title != "Auto Typer" && title != "Window Selector - Click on target window")
                    {
                        SelectedWindowHandle = rootHwnd;
                        SelectedWindowTitle = title;

                        if (string.IsNullOrEmpty(SelectedWindowTitle))
                        {
                            SelectedWindowTitle = $"Window Handle: {rootHwnd}";
                        }

                        this.Invoke(new Action(() =>
                        {
                            cursorTimer.Stop();
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }));

                        return (IntPtr)1; // Block the click
                    }
                }
            }
            return CallNextHookEx(mouseHookHandle, nCode, wParam, lParam);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        private void CursorTimer_Tick(object? sender, EventArgs e)
        {
            NativeMethods.POINT cursorPos;
            NativeMethods.GetCursorPos(out cursorPos);
            IntPtr hwnd = NativeMethods.WindowFromPoint(cursorPos);

            if (hwnd != IntPtr.Zero)
            {
                StringBuilder sb = new StringBuilder(256);
                NativeMethods.GetWindowText(hwnd, sb, sb.Capacity);
                string title = sb.ToString();

                if (!string.IsNullOrEmpty(title))
                {
                    lblInstruction.Text = $"Window under cursor:\n\"{title}\"\n\nClick LEFT mouse button | ESC to cancel";
                    lblInstruction.BackColor = Color.LightGreen;
                }
                else
                {
                    lblInstruction.Text = "1. Move mouse over target window\n2. Click LEFT mouse button to select\n\nESC - Cancel";
                    lblInstruction.BackColor = Color.White;
                }
            }
        }

        private void WindowSelectorForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                cursorTimer.Stop();
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (mouseHookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(mouseHookHandle);
            }
            if (cursorTimer != null)
            {
                cursorTimer.Stop();
                cursorTimer.Dispose();
            }
            base.OnFormClosing(e);
        }
    }
}
