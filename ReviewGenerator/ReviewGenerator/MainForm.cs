using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;

namespace ReviewGenerator
{
    public partial class MainForm : Form
    {
        private List<string> selectedImagePaths = new List<string>();
        private string API_KEY = string.Empty;
        private string API_ENDPOINT = string.Empty;
        private string MODEL = string.Empty;

        public MainForm()
        {
            InitializeComponent();
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            try
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                API_KEY = configuration["ApiSettings:ApiKey"] ?? "sk-b19e78f2c9941966-f71c0b-1406052b";
                API_ENDPOINT = configuration["ApiSettings:ApiEndpoint"] ?? "http://localhost:20128/v1";
                MODEL = configuration["ApiSettings:Model"] ?? "kr/claude-sonnet-4.5";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load configuration: {ex.Message}\nUsing default settings.", 
                    "Configuration Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                
                API_KEY = "sk-b19e78f2c9941966-f71c0b-1406052b";
                API_ENDPOINT = "http://localhost:20128/v1";
                MODEL = "kr/claude-sonnet-4.5";
            }
        }

        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.V)
            {
                PasteImageFromClipboard();
                e.Handled = true;
            }
        }

        private void PasteImageFromClipboard()
        {
            if (Clipboard.ContainsImage())
            {
                if (selectedImagePaths.Count >= 5)
                {
                    MessageBox.Show("Maximum 5 images allowed!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    Image clipboardImage = Clipboard.GetImage();
                    
                    // Save clipboard image to temp file
                    string tempPath = Path.Combine(Path.GetTempPath(), $"clipboard_image_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                    clipboardImage.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);
                    
                    selectedImagePaths.Add(tempPath);
                    AddImagePreview(tempPath);
                    
                    MessageBox.Show("Image pasted from clipboard!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to paste image from clipboard: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("No image found in clipboard!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Review Generator";
            this.Size = new Size(800, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.BackColor = Color.FromArgb(17, 24, 39);
            this.MaximizeBox = false;
            this.AutoScroll = false;

            Panel container = new Panel();
            container.Location = new Point(20, 20);
            container.Size = new Size(740, 590);
            container.BackColor = Color.FromArgb(17, 24, 39);
            container.AutoScroll = false;
            this.Controls.Add(container);

            Label lblTitle = new Label();
            lblTitle.Text = "Review Generator";
            lblTitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(243, 244, 246);
            lblTitle.Location = new Point(0, 0);
            lblTitle.AutoSize = true;
            container.Controls.Add(lblTitle);

            Label lblSubtitle = new Label();
            lblSubtitle.Text = "AI-powered review generation with OCR";
            lblSubtitle.Font = new Font("Segoe UI", 9);
            lblSubtitle.ForeColor = Color.FromArgb(156, 163, 175);
            lblSubtitle.Location = new Point(0, 28);
            lblSubtitle.AutoSize = true;
            container.Controls.Add(lblSubtitle);

            Panel divider1 = new Panel();
            divider1.Location = new Point(0, 50);
            divider1.Size = new Size(740, 1);
            divider1.BackColor = Color.FromArgb(55, 65, 81);
            container.Controls.Add(divider1);

            Label lblImage = new Label();
            lblImage.Text = "Images (up to 5) - Press Ctrl+V to paste";
            lblImage.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblImage.ForeColor = Color.FromArgb(243, 244, 246);
            lblImage.Location = new Point(0, 60);
            lblImage.AutoSize = true;
            container.Controls.Add(lblImage);

            FlowLayoutPanel imagePanel = new FlowLayoutPanel();
            imagePanel.Name = "imagePanel";
            imagePanel.Location = new Point(0, 82);
            imagePanel.Size = new Size(740, 120);
            imagePanel.BackColor = Color.FromArgb(31, 41, 55);
            imagePanel.BorderStyle = BorderStyle.FixedSingle;
            imagePanel.AutoScroll = true;
            imagePanel.WrapContents = true;
            container.Controls.Add(imagePanel);

            Button btnUpload = new Button();
            btnUpload.Text = "Add Images";
            btnUpload.Location = new Point(0, 210);
            btnUpload.Size = new Size(120, 35);
            btnUpload.BackColor = Color.FromArgb(59, 130, 246);
            btnUpload.ForeColor = Color.White;
            btnUpload.FlatStyle = FlatStyle.Flat;
            btnUpload.FlatAppearance.BorderSize = 0;
            btnUpload.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnUpload.Cursor = Cursors.Hand;
            btnUpload.Click += BtnUpload_Click;
            container.Controls.Add(btnUpload);

            Button btnClearImages = new Button();
            btnClearImages.Text = "Clear All";
            btnClearImages.Location = new Point(130, 210);
            btnClearImages.Size = new Size(100, 35);
            btnClearImages.BackColor = Color.FromArgb(239, 68, 68);
            btnClearImages.ForeColor = Color.White;
            btnClearImages.FlatStyle = FlatStyle.Flat;
            btnClearImages.FlatAppearance.BorderSize = 0;
            btnClearImages.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnClearImages.Cursor = Cursors.Hand;
            btnClearImages.Click += BtnClearImages_Click;
            container.Controls.Add(btnClearImages);

            Label lblType = new Label();
            lblType.Text = "Service Type";
            lblType.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblType.ForeColor = Color.FromArgb(243, 244, 246);
            lblType.Location = new Point(0, 255);
            lblType.AutoSize = true;
            container.Controls.Add(lblType);

            ComboBox cmbType = new ComboBox();
            cmbType.Name = "cmbType";
            cmbType.Location = new Point(0, 277);
            cmbType.Size = new Size(250, 28);
            cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbType.BackColor = Color.FromArgb(31, 41, 55);
            cmbType.ForeColor = Color.FromArgb(229, 231, 235);
            cmbType.Font = new Font("Segoe UI", 9);
            cmbType.Items.AddRange(new object[] { 
                "Restaurant / Cafe", 
                "Shop / Store", 
                "Service / Repair", 
                "Hotel / Accommodation",
                "Medical / Healthcare",
                "Beauty / Salon",
                "Other"
            });
            cmbType.SelectedIndex = 0;
            container.Controls.Add(cmbType);

            Label lblInstructions = new Label();
            lblInstructions.Text = "Additional Instructions (optional)";
            lblInstructions.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblInstructions.ForeColor = Color.FromArgb(243, 244, 246);
            lblInstructions.Location = new Point(0, 315);
            lblInstructions.AutoSize = true;
            container.Controls.Add(lblInstructions);

            TextBox txtInstructions = new TextBox();
            txtInstructions.Name = "txtInstructions";
            txtInstructions.Multiline = true;
            txtInstructions.ScrollBars = ScrollBars.Vertical;
            txtInstructions.Location = new Point(0, 337);
            txtInstructions.Size = new Size(740, 50);
            txtInstructions.Font = new Font("Segoe UI", 9);
            txtInstructions.BackColor = Color.FromArgb(31, 41, 55);
            txtInstructions.ForeColor = Color.FromArgb(229, 231, 235);
            txtInstructions.BorderStyle = BorderStyle.FixedSingle;
            container.Controls.Add(txtInstructions);

            Button btnGenerate = new Button();
            btnGenerate.Name = "btnGenerate";
            btnGenerate.Text = "Generate Review";
            btnGenerate.Location = new Point(0, 397);
            btnGenerate.Size = new Size(740, 40);
            btnGenerate.BackColor = Color.FromArgb(16, 185, 129);
            btnGenerate.ForeColor = Color.White;
            btnGenerate.FlatStyle = FlatStyle.Flat;
            btnGenerate.FlatAppearance.BorderSize = 0;
            btnGenerate.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnGenerate.Cursor = Cursors.Hand;
            btnGenerate.Click += BtnGenerate_Click;
            container.Controls.Add(btnGenerate);

            Label lblResult = new Label();
            lblResult.Text = "Generated Review";
            lblResult.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblResult.ForeColor = Color.FromArgb(243, 244, 246);
            lblResult.Location = new Point(0, 447);
            lblResult.AutoSize = true;
            container.Controls.Add(lblResult);

            TextBox txtResult = new TextBox();
            txtResult.Name = "txtResult";
            txtResult.Multiline = true;
            txtResult.ReadOnly = true;
            txtResult.ScrollBars = ScrollBars.Vertical;
            txtResult.Location = new Point(0, 469);
            txtResult.Size = new Size(620, 110);
            txtResult.Font = new Font("Segoe UI", 9);
            txtResult.BackColor = Color.FromArgb(31, 41, 55);
            txtResult.ForeColor = Color.FromArgb(229, 231, 235);
            txtResult.BorderStyle = BorderStyle.FixedSingle;
            container.Controls.Add(txtResult);

            Button btnCopy = new Button();
            btnCopy.Text = "Copy";
            btnCopy.Location = new Point(630, 469);
            btnCopy.Size = new Size(110, 110);
            btnCopy.BackColor = Color.FromArgb(59, 130, 246);
            btnCopy.ForeColor = Color.White;
            btnCopy.FlatStyle = FlatStyle.Flat;
            btnCopy.FlatAppearance.BorderSize = 0;
            btnCopy.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnCopy.Cursor = Cursors.Hand;
            btnCopy.Click += BtnCopy_Click;
            container.Controls.Add(btnCopy);
        }

        private void BtnUpload_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                ofd.Title = "Select images";
                ofd.Multiselect = true;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    int addedCount = 0;
                    int skippedCount = 0;
                    
                    foreach (string file in ofd.FileNames)
                    {
                        if (selectedImagePaths.Count >= 5)
                        {
                            skippedCount = ofd.FileNames.Length - addedCount;
                            break;
                        }

                        if (!selectedImagePaths.Contains(file))
                        {
                            // Validate file size (max 10MB)
                            FileInfo fileInfo = new FileInfo(file);
                            if (fileInfo.Length > 10 * 1024 * 1024)
                            {
                                MessageBox.Show($"Image {Path.GetFileName(file)} is too large (max 10MB).", 
                                    "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                skippedCount++;
                                continue;
                            }
                            
                            selectedImagePaths.Add(file);
                            AddImagePreview(file);
                            addedCount++;
                        }
                        else
                        {
                            skippedCount++;
                        }
                    }
                    
                    if (skippedCount > 0)
                    {
                        MessageBox.Show($"Added {addedCount} image(s). {skippedCount} image(s) skipped (maximum 5 images allowed or already added).", 
                            "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void AddImagePreview(string imagePath)
        {
            var container = this.Controls[0] as Panel;
            if (container == null) return;
            
            var imagePanel = container.Controls["imagePanel"] as FlowLayoutPanel;
            if (imagePanel == null) return;

            Panel previewContainer = new Panel();
            previewContainer.Size = new Size(100, 110);
            previewContainer.Margin = new Padding(3);
            previewContainer.BackColor = Color.FromArgb(55, 65, 81);

            PictureBox pic = new PictureBox();
            pic.Size = new Size(100, 85);
            pic.Location = new Point(0, 0);
            pic.SizeMode = PictureBoxSizeMode.Zoom;
            
            try
            {
                pic.Image = Image.FromFile(imagePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            pic.Tag = imagePath;
            previewContainer.Controls.Add(pic);

            Button btnRemove = new Button();
            btnRemove.Text = "Remove";
            btnRemove.Size = new Size(100, 25);
            btnRemove.Location = new Point(0, 85);
            btnRemove.BackColor = Color.FromArgb(239, 68, 68);
            btnRemove.ForeColor = Color.White;
            btnRemove.FlatStyle = FlatStyle.Flat;
            btnRemove.FlatAppearance.BorderSize = 0;
            btnRemove.Font = new Font("Segoe UI", 7, FontStyle.Bold);
            btnRemove.Cursor = Cursors.Hand;
            btnRemove.Tag = imagePath;
            btnRemove.Click += (s, ev) =>
            {
                if (s is Button btn && btn.Tag is string path)
                {
                    selectedImagePaths.Remove(path);
                    imagePanel.Controls.Remove(previewContainer);
                    pic.Image?.Dispose();
                }
            };
            previewContainer.Controls.Add(btnRemove);

            imagePanel.Controls.Add(previewContainer);
        }

        private void BtnClearImages_Click(object? sender, EventArgs e)
        {
            var container = this.Controls[0] as Panel;
            if (container == null) return;
            
            var imagePanel = container.Controls["imagePanel"] as FlowLayoutPanel;
            if (imagePanel == null) return;

            foreach (Control ctrl in imagePanel.Controls)
            {
                if (ctrl is Panel panel)
                {
                    foreach (Control innerCtrl in panel.Controls)
                    {
                        if (innerCtrl is PictureBox pic)
                        {
                            pic.Image?.Dispose();
                        }
                    }
                }
            }

            imagePanel.Controls.Clear();
            selectedImagePaths.Clear();
        }

        private async void BtnGenerate_Click(object? sender, EventArgs e)
        {
            var container = this.Controls[0] as Panel;
            if (container == null) return;
            
            var cmbType = container.Controls["cmbType"] as ComboBox;
            var txtInstructions = container.Controls["txtInstructions"] as TextBox;
            var txtResult = container.Controls["txtResult"] as TextBox;
            var btnGenerate = container.Controls["btnGenerate"] as Button;
            
            if (cmbType == null || txtInstructions == null || txtResult == null || 
                btnGenerate == null) return;

            if (selectedImagePaths.Count == 0)
            {
                MessageBox.Show("Please upload at least one image first!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnGenerate.Enabled = false;
            btnGenerate.Text = "Generating...";
            txtResult.Text = "Generating review...";

            try
            {
                string serviceType = cmbType.SelectedItem?.ToString() ?? "Unknown";
                string instructions = txtInstructions.Text ?? string.Empty;
                string review = await GenerateReview(selectedImagePaths, serviceType, instructions);
                txtResult.Text = review;
            }
            catch (Exception ex)
            {
                txtResult.Text = $"Error: {ex.Message}";
                MessageBox.Show($"Error generating review: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnGenerate.Enabled = true;
                btnGenerate.Text = "Generate Review";
            }
        }

        private void BtnCopy_Click(object? sender, EventArgs e)
        {
            var container = this.Controls[0] as Panel;
            if (container == null) return;
            
            var txtResult = container.Controls["txtResult"] as TextBox;
            if (txtResult == null) return;

            if (!string.IsNullOrEmpty(txtResult.Text))
            {
                Clipboard.SetText(txtResult.Text);
                MessageBox.Show("Review copied to clipboard!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async Task<string> GenerateReview(List<string> imagePaths, string serviceType, string instructions)
        {
            // Extract text from all images using OCR
            StringBuilder extractedText = new StringBuilder();
            bool hasValidText = false;
            
            foreach (string imagePath in imagePaths)
            {
                try
                {
                    string text = ExtractTextFromImage(imagePath);
                    extractedText.AppendLine($"--- Image: {Path.GetFileName(imagePath)} ---");
                    extractedText.AppendLine(text);
                    extractedText.AppendLine();
                    
                    // Check if we got valid text (not just error messages)
                    if (!text.Contains("[OCR Error") && !text.Contains("[No text detected"))
                    {
                        hasValidText = true;
                    }
                }
                catch (Exception ex)
                {
                    extractedText.AppendLine($"Error reading image {Path.GetFileName(imagePath)}: {ex.Message}");
                }
            }

            string prompt;
            
            if (hasValidText)
            {
                prompt = $@"You are a helpful assistant that generates authentic and detailed reviews based on extracted text from images.

Service Type: {serviceType}
{(string.IsNullOrEmpty(instructions) ? "" : $"Additional Instructions: {instructions}")}

Extracted text from images:
{extractedText}

Based on the extracted text above (from receipts, business cards, or other documents), generate a detailed and authentic review in Russian language. 

The review should:
- Be natural and conversational
- Include specific details from the extracted text if visible (prices, items, location, business name, etc.)
- Be positive but realistic
- Be 3-5 sentences long
- Sound like a real customer wrote it

Generate ONLY the review text, without any additional explanations or formatting.";
            }
            else
            {
                // If OCR failed, generate review based on service type only
                prompt = $@"You are a helpful assistant that generates authentic and detailed reviews.

Service Type: {serviceType}
{(string.IsNullOrEmpty(instructions) ? "" : $"Additional Instructions: {instructions}")}

Note: Unable to extract text from the provided images. Please generate a generic but authentic review for this type of service in Russian language.

The review should:
- Be natural and conversational
- Be positive but realistic
- Be 3-5 sentences long
- Sound like a real customer wrote it
- Mention typical aspects of this service type

Generate ONLY the review text, without any additional explanations or formatting.";
            }

            var requestBody = new
            {
                model = MODEL,
                max_tokens = 1024,
                stream = false,  // Disable streaming
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                }
            };

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(60);
                client.DefaultRequestHeaders.Add("x-api-key", API_KEY);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                string responseText;
                
                try
                {
                    response = await client.PostAsync($"{API_ENDPOINT}/messages", content);
                    responseText = await response.Content.ReadAsStringAsync();
                }
                catch (HttpRequestException ex)
                {
                    throw new Exception($"Network error: Unable to connect to API endpoint. Please check your connection and API settings.\n\nDetails: {ex.Message}");
                }
                catch (TaskCanceledException)
                {
                    throw new Exception("Request timeout: The API took too long to respond. Please try again.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage = $"API Error ({response.StatusCode})";
                    
                    try
                    {
                        var errorDoc = JsonDocument.Parse(responseText);
                        if (errorDoc.RootElement.TryGetProperty("error", out var errorProp))
                        {
                            if (errorProp.TryGetProperty("message", out var messageProp))
                            {
                                errorMessage += $": {messageProp.GetString()}";
                            }
                        }
                    }
                    catch
                    {
                        errorMessage += $": {responseText}";
                    }
                    
                    throw new Exception(errorMessage);
                }

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    throw new Exception("API returned empty response");
                }

                try
                {
                    var jsonDoc = JsonDocument.Parse(responseText);
                    var reviewText = jsonDoc.RootElement
                        .GetProperty("content")[0]
                        .GetProperty("text")
                        .GetString();

                    return reviewText ?? "Failed to generate review";
                }
                catch (JsonException ex)
                {
                    throw new Exception($"Failed to parse API response. The response format may be invalid.\n\nDetails: {ex.Message}");
                }
                catch (KeyNotFoundException)
                {
                    throw new Exception("API response missing expected fields. The API may have returned an unexpected format.");
                }
            }
        }

        private string ExtractTextFromImage(string imagePath)
        {
            try
            {
                if (!File.Exists(imagePath))
                {
                    return "[Error: Image file not found]";
                }

                // Use Windows OCR (async method called synchronously)
                var task = Task.Run(async () => await ExtractTextFromImageAsync(imagePath));
                return task.Result;
            }
            catch (Exception ex)
            {
                return $"[OCR Error: {ex.Message}]";
            }
        }

        private async Task<string> ExtractTextFromImageAsync(string imagePath)
        {
            try
            {
                // Load image file
                StorageFile file = await StorageFile.GetFileFromPathAsync(imagePath);
                
                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
                {
                    // Create decoder
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                    
                    // Get bitmap
                    SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                    
                    // Create OCR engine for Russian and English
                    OcrEngine ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
                    
                    if (ocrEngine == null)
                    {
                        // Fallback to English if user profile languages not available
                        var language = new Windows.Globalization.Language("en");
                        ocrEngine = OcrEngine.TryCreateFromLanguage(language);
                    }
                    
                    if (ocrEngine == null)
                    {
                        return "[Error: OCR engine not available on this system]";
                    }
                    
                    // Perform OCR
                    OcrResult result = await ocrEngine.RecognizeAsync(softwareBitmap);
                    
                    if (result == null || result.Lines.Count == 0)
                    {
                        return "[No text detected in image]";
                    }
                    
                    // Extract text from all lines
                    StringBuilder text = new StringBuilder();
                    foreach (var line in result.Lines)
                    {
                        text.AppendLine(line.Text);
                    }
                    
                    return text.ToString().Trim();
                }
            }
            catch (Exception ex)
            {
                return $"[OCR Error: {ex.Message}]";
            }
        }
    }
}
