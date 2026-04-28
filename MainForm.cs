using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoTyper
{
    public partial class MainForm : Form
    {
        private IntPtr targetWindowHandle = IntPtr.Zero;
        private string targetWindowTitle = "";
        private CancellationTokenSource? cancellationTokenSource;
        private bool isTyping = false;

        public MainForm()
        {
            InitializeComponent();
            RegisterHotKey(this.Handle, 1, 0, (int)Keys.F9);
        }

        private void InitializeComponent()
        {
            this.Text = "Auto Typer";
            this.Size = new Size(650, 580);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.BackColor = Color.FromArgb(17, 24, 39);
            this.MaximizeBox = false;

            // Main container with padding
            Panel container = new Panel();
            container.Location = new Point(30, 30);
            container.Size = new Size(590, 520);
            container.BackColor = Color.FromArgb(17, 24, 39);
            this.Controls.Add(container);

            // Title
            Label lblTitle = new Label();
            lblTitle.Text = "Auto Typer";
            lblTitle.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(243, 244, 246);
            lblTitle.Location = new Point(0, 0);
            lblTitle.AutoSize = true;
            container.Controls.Add(lblTitle);

            Label lblSubtitle = new Label();
            lblSubtitle.Text = "Automate your typing";
            lblSubtitle.Font = new Font("Segoe UI", 9);
            lblSubtitle.ForeColor = Color.FromArgb(156, 163, 175);
            lblSubtitle.Location = new Point(0, 35);
            lblSubtitle.AutoSize = true;
            container.Controls.Add(lblSubtitle);

            // Divider
            Panel divider1 = new Panel();
            divider1.Location = new Point(0, 60);
            divider1.Size = new Size(590, 1);
            divider1.BackColor = Color.FromArgb(55, 65, 81);
            container.Controls.Add(divider1);

            // Text input section
            Label lblText = new Label();
            lblText.Text = "Text to type";
            lblText.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblText.ForeColor = Color.FromArgb(243, 244, 246);
            lblText.Location = new Point(0, 75);
            lblText.AutoSize = true;
            container.Controls.Add(lblText);

            TextBox txtInput = new TextBox();
            txtInput.Name = "txtInput";
            txtInput.Multiline = true;
            txtInput.ScrollBars = ScrollBars.Vertical;
            txtInput.Location = new Point(0, 100);
            txtInput.Size = new Size(590, 120);
            txtInput.Font = new Font("Consolas", 9);
            txtInput.BackColor = Color.FromArgb(31, 41, 55);
            txtInput.ForeColor = Color.FromArgb(229, 231, 235);
            txtInput.BorderStyle = BorderStyle.FixedSingle;
            container.Controls.Add(txtInput);

            // Target window section
            Label lblWindow = new Label();
            lblWindow.Text = "Target window";
            lblWindow.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblWindow.ForeColor = Color.FromArgb(243, 244, 246);
            lblWindow.Location = new Point(0, 235);
            lblWindow.AutoSize = true;
            container.Controls.Add(lblWindow);

            TextBox txtWindow = new TextBox();
            txtWindow.Name = "txtWindow";
            txtWindow.Location = new Point(0, 260);
            txtWindow.Size = new Size(420, 30);
            txtWindow.ReadOnly = true;
            txtWindow.BackColor = Color.FromArgb(31, 41, 55);
            txtWindow.ForeColor = Color.FromArgb(156, 163, 175);
            txtWindow.BorderStyle = BorderStyle.FixedSingle;
            txtWindow.Font = new Font("Segoe UI", 9);
            txtWindow.Text = "No window selected";
            container.Controls.Add(txtWindow);

            Button btnSelectWindow = new Button();
            btnSelectWindow.Text = "Select";
            btnSelectWindow.Location = new Point(430, 258);
            btnSelectWindow.Size = new Size(160, 34);
            btnSelectWindow.BackColor = Color.FromArgb(59, 130, 246);
            btnSelectWindow.ForeColor = Color.White;
            btnSelectWindow.FlatStyle = FlatStyle.Flat;
            btnSelectWindow.FlatAppearance.BorderSize = 0;
            btnSelectWindow.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnSelectWindow.Cursor = Cursors.Hand;
            btnSelectWindow.Click += BtnSelectWindow_Click;
            container.Controls.Add(btnSelectWindow);

            // Delay section
            Label lblDelay = new Label();
            lblDelay.Text = "Typing speed";
            lblDelay.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblDelay.ForeColor = Color.FromArgb(243, 244, 246);
            lblDelay.Location = new Point(0, 310);
            lblDelay.AutoSize = true;
            container.Controls.Add(lblDelay);

            NumericUpDown numDelay = new NumericUpDown();
            numDelay.Name = "numDelay";
            numDelay.Location = new Point(0, 335);
            numDelay.Size = new Size(100, 25);
            numDelay.Minimum = 0;
            numDelay.Maximum = 5000;
            numDelay.Value = 50;
            numDelay.Font = new Font("Segoe UI", 9);
            numDelay.BackColor = Color.FromArgb(31, 41, 55);
            numDelay.ForeColor = Color.FromArgb(229, 231, 235);
            numDelay.BorderStyle = BorderStyle.FixedSingle;
            container.Controls.Add(numDelay);

            Label lblMs = new Label();
            lblMs.Text = "ms between characters";
            lblMs.Font = new Font("Segoe UI", 8);
            lblMs.ForeColor = Color.FromArgb(156, 163, 175);
            lblMs.Location = new Point(110, 340);
            lblMs.AutoSize = true;
            container.Controls.Add(lblMs);

            // Divider
            Panel divider2 = new Panel();
            divider2.Location = new Point(0, 375);
            divider2.Size = new Size(590, 1);
            divider2.BackColor = Color.FromArgb(55, 65, 81);
            container.Controls.Add(divider2);

            // Status
            Label lblStatus = new Label();
            lblStatus.Name = "lblStatus";
            lblStatus.Text = "Ready";
            lblStatus.Location = new Point(0, 390);
            lblStatus.Size = new Size(590, 20);
            lblStatus.Font = new Font("Segoe UI", 9);
            lblStatus.ForeColor = Color.FromArgb(16, 185, 129);
            container.Controls.Add(lblStatus);

            // Control buttons
            Button btnStart = new Button();
            btnStart.Name = "btnStart";
            btnStart.Text = "Start Typing";
            btnStart.Location = new Point(0, 425);
            btnStart.Size = new Size(285, 42);
            btnStart.BackColor = Color.FromArgb(16, 185, 129);
            btnStart.ForeColor = Color.White;
            btnStart.FlatStyle = FlatStyle.Flat;
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnStart.Cursor = Cursors.Hand;
            btnStart.Click += BtnStart_Click;
            container.Controls.Add(btnStart);

            Button btnStop = new Button();
            btnStop.Name = "btnStop";
            btnStop.Text = "Stop";
            btnStop.Location = new Point(305, 425);
            btnStop.Size = new Size(285, 42);
            btnStop.BackColor = Color.FromArgb(239, 68, 68);
            btnStop.ForeColor = Color.White;
            btnStop.FlatStyle = FlatStyle.Flat;
            btnStop.FlatAppearance.BorderSize = 0;
            btnStop.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnStop.Cursor = Cursors.Hand;
            btnStop.Enabled = false;
            btnStop.Click += BtnStop_Click;
            container.Controls.Add(btnStop);

            // Hotkey hint
            Label lblHint = new Label();
            lblHint.Text = "Press F9 to start/stop";
            lblHint.Location = new Point(0, 480);
            lblHint.Size = new Size(590, 18);
            lblHint.Font = new Font("Segoe UI", 8);
            lblHint.ForeColor = Color.FromArgb(107, 114, 128);
            lblHint.TextAlign = ContentAlignment.MiddleCenter;
            container.Controls.Add(lblHint);
        }

        private void BtnSelectWindow_Click(object? sender, EventArgs e)
        {
            var container = this.Controls[0] as Panel;
            var txtWindow = container.Controls["txtWindow"] as TextBox;
            var lblStatus = container.Controls["lblStatus"] as Label;

            lblStatus.Text = "Click on target window...";
            lblStatus.ForeColor = Color.FromArgb(251, 146, 60);

            var selectForm = new WindowSelectorForm();
            selectForm.Owner = this;
            if (selectForm.ShowDialog() == DialogResult.OK)
            {
                targetWindowHandle = selectForm.SelectedWindowHandle;
                targetWindowTitle = selectForm.SelectedWindowTitle;
                txtWindow.Text = targetWindowTitle;
                lblStatus.Text = $"Selected: {targetWindowTitle}";
                lblStatus.ForeColor = Color.FromArgb(16, 185, 129);
            }
            else
            {
                lblStatus.Text = "Selection cancelled";
                lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
            }
        }

        private async void BtnStart_Click(object? sender, EventArgs e)
        {
            await StartTyping();
        }

        private void BtnStop_Click(object? sender, EventArgs e)
        {
            StopTyping();
        }

        private async Task StartTyping()
        {
            var container = this.Controls[0] as Panel;
            var txtInput = container.Controls["txtInput"] as TextBox;
            var numDelay = container.Controls["numDelay"] as NumericUpDown;
            var lblStatus = container.Controls["lblStatus"] as Label;
            var btnStart = container.Controls["btnStart"] as Button;
            var btnStop = container.Controls["btnStop"] as Button;

            if (string.IsNullOrEmpty(txtInput.Text))
            {
                MessageBox.Show("Введите текст для ввода!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (targetWindowHandle == IntPtr.Zero)
            {
                MessageBox.Show("Выберите целевое окно!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            isTyping = true;
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            lblStatus.Text = "Typing... (Don't click this window!)";
            lblStatus.ForeColor = Color.FromArgb(251, 146, 60);

            // Set window to always on top during typing
            this.TopMost = true;

            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            // Activate target window ONCE before starting
            NativeMethods.SetForegroundWindow(targetWindowHandle);
            await Task.Delay(800); // Wait for activation

            try
            {
                await Task.Run(() =>
                {
                    string text = txtInput.Text;
                    int delay = (int)numDelay.Value;

                    foreach (char c in text)
                    {
                        if (token.IsCancellationRequested)
                            break;

                        KeyboardSimulator.SendChar(c);
                        Thread.Sleep(delay);
                    }
                }, token);

                if (!token.IsCancellationRequested)
                {
                    lblStatus.Text = "Completed successfully";
                    lblStatus.ForeColor = Color.FromArgb(16, 185, 129);
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error: {ex.Message}";
                lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
            }
            finally
            {
                isTyping = false;
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                
                // Remove always on top
                this.TopMost = false;
            }
        }

        private void StopTyping()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                var container = this.Controls[0] as Panel;
                var lblStatus = container.Controls["lblStatus"] as Label;
                lblStatus.Text = "Stopped by user";
                lblStatus.ForeColor = Color.FromArgb(251, 146, 60);
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY)
            {
                if (isTyping)
                    StopTyping();
                else
                    _ = StartTyping();
            }
            base.WndProc(ref m);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            UnregisterHotKey(this.Handle, 1);
            base.OnFormClosing(e);
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
