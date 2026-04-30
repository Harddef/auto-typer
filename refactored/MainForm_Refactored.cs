using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static AutoTyper.HumanizedKeyboardSimulator;

namespace AutoTyper
{
    public partial class MainForm : Form
    {
        private IntPtr targetWindowHandle = IntPtr.Zero;
        private string targetWindowTitle = "";
        private CancellationTokenSource? cancellationTokenSource;
        private readonly SemaphoreSlim typingLock = new SemaphoreSlim(1, 1);
        private bool isTyping = false;
        private TypingSettings currentSettings = TypingSettings.GetProfile(TypingProfile.Natural);

        public MainForm()
        {
            InitializeComponent();
            RegisterHotKey(this.Handle, 1, 0, (int)Keys.F9);
        }

        private void InitializeComponent()
        {
            this.Text = "Auto Typer Pro";
            this.Size = new Size(700, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.BackColor = Color.FromArgb(17, 24, 39);
            this.MaximizeBox = false;

            Panel container = new Panel();
            container.Location = new Point(30, 30);
            container.Size = new Size(640, 620);
            container.BackColor = Color.FromArgb(17, 24, 39);
            this.Controls.Add(container);

            // Title
            Label lblTitle = new Label();
            lblTitle.Text = "Auto Typer Pro";
            lblTitle.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(243, 244, 246);
            lblTitle.Location = new Point(0, 0);
            lblTitle.AutoSize = true;
            container.Controls.Add(lblTitle);

            Label lblSubtitle = new Label();
            lblSubtitle.Text = "Advanced typing automation with humanization";
            lblSubtitle.Font = new Font("Segoe UI", 9);
            lblSubtitle.ForeColor = Color.FromArgb(156, 163, 175);
            lblSubtitle.Location = new Point(0, 35);
            lblSubtitle.AutoSize = true;
            container.Controls.Add(lblSubtitle);

            Panel divider1 = new Panel();
            divider1.Location = new Point(0, 60);
            divider1.Size = new Size(640, 1);
            divider1.BackColor = Color.FromArgb(55, 65, 81);
            container.Controls.Add(divider1);

            // Text input
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
            txtInput.Size = new Size(640, 120);
            txtInput.Font = new Font("Consolas", 9);
            txtInput.BackColor = Color.FromArgb(31, 41, 55);
            txtInput.ForeColor = Color.FromArgb(229, 231, 235);
            txtInput.BorderStyle = BorderStyle.FixedSingle;
            container.Controls.Add(txtInput);

            // Target window
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
            txtWindow.Size = new Size(470, 30);
            txtWindow.ReadOnly = true;
            txtWindow.BackColor = Color.FromArgb(31, 41, 55);
            txtWindow.ForeColor = Color.FromArgb(156, 163, 175);
            txtWindow.BorderStyle = BorderStyle.FixedSingle;
            txtWindow.Font = new Font("Segoe UI", 9);
            txtWindow.Text = "No window selected";
            container.Controls.Add(txtWindow);

            Button btnSelectWindow = new Button();
            btnSelectWindow.Text = "Select";
            btnSelectWindow.Location = new Point(480, 258);
            btnSelectWindow.Size = new Size(160, 34);
            btnSelectWindow.BackColor = Color.FromArgb(59, 130, 246);
            btnSelectWindow.ForeColor = Color.White;
            btnSelectWindow.FlatStyle = FlatStyle.Flat;
            btnSelectWindow.FlatAppearance.BorderSize = 0;
            btnSelectWindow.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnSelectWindow.Cursor = Cursors.Hand;
            btnSelectWindow.Click += BtnSelectWindow_Click;
            container.Controls.Add(btnSelectWindow);

            // Typing Profile
            Label lblProfile = new Label();
            lblProfile.Text = "Typing profile";
            lblProfile.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblProfile.ForeColor = Color.FromArgb(243, 244, 246);
            lblProfile.Location = new Point(0, 310);
            lblProfile.AutoSize = true;
            container.Controls.Add(lblProfile);

            ComboBox cmbProfile = new ComboBox();
            cmbProfile.Name = "cmbProfile";
            cmbProfile.Location = new Point(0, 335);
            cmbProfile.Size = new Size(200, 30);
            cmbProfile.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbProfile.BackColor = Color.FromArgb(31, 41, 55);
            cmbProfile.ForeColor = Color.FromArgb(229, 231, 235);
            cmbProfile.Font = new Font("Segoe UI", 9);
            cmbProfile.Items.AddRange(new object[] { 
                "Fast & Risky (30-50ms)",
                "Natural (50-80ms) ✓",
                "Super Safe (80-150ms)"
            });
            cmbProfile.SelectedIndex = 1;
            cmbProfile.SelectedIndexChanged += CmbProfile_SelectedIndexChanged;
            container.Controls.Add(cmbProfile);

            // Humanization options
            CheckBox chkHumanize = new CheckBox();
            chkHumanize.Name = "chkHumanize";
            chkHumanize.Text = "Enable humanization (variable speed, pauses)";
            chkHumanize.Location = new Point(0, 375);
            chkHumanize.Size = new Size(320, 20);
            chkHumanize.ForeColor = Color.FromArgb(229, 231, 235);
            chkHumanize.Font = new Font("Segoe UI", 9);
            chkHumanize.Checked = true;
            chkHumanize.CheckedChanged += ChkHumanize_CheckedChanged;
            container.Controls.Add(chkHumanize);

            CheckBox chkTypos = new CheckBox();
            chkTypos.Name = "chkTypos";
            chkTypos.Text = "Simulate typos (2-3% error rate)";
            chkTypos.Location = new Point(0, 400);
            chkTypos.Size = new Size(320, 20);
            chkTypos.ForeColor = Color.FromArgb(229, 231, 235);
            chkTypos.Font = new Font("Segoe UI", 9);
            chkTypos.Checked = true;
            chkTypos.CheckedChanged += ChkTypos_CheckedChanged;
            container.Controls.Add(chkTypos);

            Panel divider2 = new Panel();
            divider2.Location = new Point(0, 435);
            divider2.Size = new Size(640, 1);
            divider2.BackColor = Color.FromArgb(55, 65, 81);
            container.Controls.Add(divider2);

            // Progress bar
            ProgressBar progressBar = new ProgressBar();
            progressBar.Name = "progressBar";
            progressBar.Location = new Point(0, 450);
            progressBar.Size = new Size(640, 8);
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Visible = false;
            container.Controls.Add(progressBar);

            // Status
            Label lblStatus = new Label();
            lblStatus.Name = "lblStatus";
            lblStatus.Text = "Ready";
            lblStatus.Location = new Point(0, 465);
            lblStatus.Size = new Size(640, 20);
            lblStatus.Font = new Font("Segoe UI", 9);
            lblStatus.ForeColor = Color.FromArgb(16, 185, 129);
            container.Controls.Add(lblStatus);

            // Control buttons
            Button btnStart = new Button();
            btnStart.Name = "btnStart";
            btnStart.Text = "Start Typing (F9)";
            btnStart.Location = new Point(0, 500);
            btnStart.Size = new Size(310, 42);
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
            btnStop.Text = "Stop (F9)";
            btnStop.Location = new Point(330, 500);
            btnStop.Size = new Size(310, 42);
            btnStop.BackColor = Color.FromArgb(239, 68, 68);
            btnStop.ForeColor = Color.White;
            btnStop.FlatStyle = FlatStyle.Flat;
            btnStop.FlatAppearance.BorderSize = 0;
            btnStop.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnStop.Cursor = Cursors.Hand;
            btnStop.Enabled = false;
            btnStop.Click += BtnStop_Click;
            container.Controls.Add(btnStop);

            // Info labels
            Label lblInfo = new Label();
            lblInfo.Text = "⚠️ Don't click this window while typing is in progress!";
            lblInfo.Location = new Point(0, 555);
            lblInfo.Size = new Size(640, 18);
            lblInfo.Font = new Font("Segoe UI", 8);
            lblInfo.ForeColor = Color.FromArgb(251, 146, 60);
            lblInfo.TextAlign = ContentAlignment.MiddleCenter;
            container.Controls.Add(lblInfo);

            Label lblHint = new Label();
            lblHint.Text = "Press F9 to start/stop | Natural typing with humanization enabled";
            lblHint.Location = new Point(0, 580);
            lblHint.Size = new Size(640, 18);
            lblHint.Font = new Font("Segoe UI", 8);
            lblHint.ForeColor = Color.FromArgb(107, 114, 128);
            lblHint.TextAlign = ContentAlignment.MiddleCenter;
            container.Controls.Add(lblHint);
        }

        private void CmbProfile_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var cmbProfile = sender as ComboBox;
            if (cmbProfile == null) return;

            currentSettings = cmbProfile.SelectedIndex switch
            {
                0 => TypingSettings.GetProfile(TypingProfile.Fast),
                1 => TypingSettings.GetProfile(TypingProfile.Natural),
                2 => TypingSettings.GetProfile(TypingProfile.SuperSafe),
                _ => TypingSettings.GetProfile(TypingProfile.Natural)
            };

            UpdateHumanizationCheckboxes();
        }

        private void ChkHumanize_CheckedChanged(object? sender, EventArgs e)
        {
            var chk = sender as CheckBox;
            if (chk != null)
            {
                currentSettings.EnableHumanization = chk.Checked;
            }
        }

        private void ChkTypos_CheckedChanged(object? sender, EventArgs e)
        {
            var chk = sender as CheckBox;
            if (chk != null)
            {
                currentSettings.EnableTypos = chk.Checked;
            }
        }

        private void UpdateHumanizationCheckboxes()
        {
            var container = this.Controls[0] as Panel;
            var chkHumanize = container?.Controls["chkHumanize"] as CheckBox;
            var chkTypos = container?.Controls["chkTypos"] as CheckBox;

            if (chkHumanize != null)
                chkHumanize.Checked = currentSettings.EnableHumanization;
            
            if (chkTypos != null)
                chkTypos.Checked = currentSettings.EnableTypos;
        }

        private void BtnSelectWindow_Click(object? sender, EventArgs e)
        {
            var container = this.Controls[0] as Panel;
            var txtWindow = container?.Controls["txtWindow"] as TextBox;
            var lblStatus = container?.Controls["lblStatus"] as Label;

            if (lblStatus != null)
            {
                lblStatus.Text = "Click on target window...";
                lblStatus.ForeColor = Color.FromArgb(251, 146, 60);
            }

            using (var selectForm = new WindowSelectorForm())
            {
                selectForm.Owner = this;
                if (selectForm.ShowDialog() == DialogResult.OK)
                {
                    targetWindowHandle = selectForm.SelectedWindowHandle;
                    targetWindowTitle = selectForm.SelectedWindowTitle;
                    
                    if (txtWindow != null)
                        txtWindow.Text = targetWindowTitle;
                    
                    if (lblStatus != null)
                    {
                        lblStatus.Text = $"Selected: {targetWindowTitle}";
                        lblStatus.ForeColor = Color.FromArgb(16, 185, 129);
                    }
                }
                else
                {
                    if (lblStatus != null)
                    {
                        lblStatus.Text = "Selection cancelled";
                        lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
                    }
                }
            }
        }

        private async void BtnStart_Click(object? sender, EventArgs e)
        {
            await StartTypingAsync();
        }

        private void BtnStop_Click(object? sender, EventArgs e)
        {
            StopTyping();
        }

        private async Task StartTypingAsync()
        {
            // Use semaphore to prevent race conditions
            if (!await typingLock.WaitAsync(0))
            {
                return; // Already typing
            }

            try
            {
                var container = this.Controls[0] as Panel;
                var txtInput = container?.Controls["txtInput"] as TextBox;
                var lblStatus = container?.Controls["lblStatus"] as Label;
                var btnStart = container?.Controls["btnStart"] as Button;
                var btnStop = container?.Controls["btnStop"] as Button;
                var progressBar = container?.Controls["progressBar"] as ProgressBar;

                if (txtInput == null || lblStatus == null || btnStart == null || btnStop == null)
                    return;

                if (string.IsNullOrEmpty(txtInput.Text))
                {
                    MessageBox.Show("Please enter text to type!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Validate window handle
                if (!ValidateWindowHandle(targetWindowHandle))
                {
                    MessageBox.Show("Please select a valid target window!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    targetWindowHandle = IntPtr.Zero;
                    targetWindowTitle = "";
                    var txtWindow = container?.Controls["txtWindow"] as TextBox;
                    if (txtWindow != null)
                        txtWindow.Text = "No window selected";
                    return;
                }

                isTyping = true;
                btnStart.Enabled = false;
                btnStop.Enabled = true;
                
                if (progressBar != null)
                {
                    progressBar.Value = 0;
                    progressBar.Visible = true;
                }

                lblStatus.Text = "Typing... (Don't click this window!)";
                lblStatus.ForeColor = Color.FromArgb(251, 146, 60);

                this.TopMost = true;

                cancellationTokenSource = new CancellationTokenSource();
                var token = cancellationTokenSource.Token;

                // Activate target window
                NativeMethods.SetForegroundWindow(targetWindowHandle);
                await Task.Delay(800, token);

                // Validate again after delay
                if (!ValidateWindowHandle(targetWindowHandle))
                {
                    lblStatus.Text = "Target window closed!";
                    lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
                    return;
                }

                try
                {
                    var progress = new Progress<int>(percent =>
                    {
                        if (progressBar != null && !progressBar.IsDisposed)
                        {
                            progressBar.Value = Math.Min(100, Math.Max(0, percent));
                        }
                    });

                    await TypeTextAsync(txtInput.Text, currentSettings, token, progress);

                    if (!token.IsCancellationRequested)
                    {
                        lblStatus.Text = "Completed successfully ✓";
                        lblStatus.ForeColor = Color.FromArgb(16, 185, 129);
                    }
                }
                catch (OperationCanceledException)
                {
                    lblStatus.Text = "Stopped by user";
                    lblStatus.ForeColor = Color.FromArgb(251, 146, 60);
                }
                catch (Exception ex)
                {
                    lblStatus.Text = $"Error: {ex.Message}";
                    lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
                }
            }
            finally
            {
                isTyping = false;
                
                var container = this.Controls[0] as Panel;
                var btnStart = container?.Controls["btnStart"] as Button;
                var btnStop = container?.Controls["btnStop"] as Button;
                var progressBar = container?.Controls["progressBar"] as ProgressBar;

                if (btnStart != null)
                    btnStart.Enabled = true;
                
                if (btnStop != null)
                    btnStop.Enabled = false;
                
                if (progressBar != null)
                    progressBar.Visible = false;

                this.TopMost = false;

                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;

                typingLock.Release();
            }
        }

        private void StopTyping()
        {
            cancellationTokenSource?.Cancel();
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY)
            {
                if (isTyping)
                    StopTyping();
                else
                    _ = StartTypingAsync();
            }
            base.WndProc(ref m);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            UnregisterHotKey(this.Handle, 1);
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            typingLock?.Dispose();
            base.OnFormClosing(e);
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
