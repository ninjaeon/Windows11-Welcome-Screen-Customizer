using System;
using System.Windows.Forms;
using System.Security.Principal;
using Microsoft.Win32;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace WelcomeScreenCustomizer
{
    public partial class MainForm : Form
    {
        private string selectedImagePath;
        private CheckBox debugCheckBox;
        private readonly Color primaryColor = Color.FromArgb(0, 120, 212);
        private readonly Color backgroundColor = Color.FromArgb(243, 243, 243);
        private readonly Color buttonHoverColor = Color.FromArgb(0, 102, 184);
        private PictureBox previewBox;
        private Label fileLabel;
        private Label versionLabel;
        private Image placeholderImage;

        public MainForm()
        {
            InitializeComponents();
            CheckAdminRights();
            CreatePlaceholderImage();
            ShowPlaceholder();
        }

        private void InitializeComponents()
        {
            // Set form properties
            this.Text = "Windows 11 Welcome Screen Customizer";
            this.Size = new Size(800, 600);
            this.MinimumSize = new Size(800, 600);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = backgroundColor;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            // Set the application icon
            try
            {
                using (Stream iconStream = GetType().Assembly.GetManifestResourceStream("app.ico"))
                {
                    if (iconStream != null)
                    {
                        this.Icon = new Icon(iconStream);
                    }
                }
            }
            catch (Exception)
            {
                // Silently fail if icon cannot be loaded
            }

            // Create main panel with padding
            Panel mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(30),
                BackColor = backgroundColor
            };
            this.Controls.Add(mainPanel);

            // Title label
            Label titleLabel = new Label
            {
                Text = "Windows 11 Welcome Screen Customizer",
                Font = new Font("Segoe UI", 16F, FontStyle.Regular),
                ForeColor = primaryColor,
                AutoSize = false,
                Width = 740,
                Height = 40,
                Location = new Point(20, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            mainPanel.Controls.Add(titleLabel);

            // Subtitle/instruction label
            Label instructionLabel = new Label
            {
                Text = "Select an image file to set as your Windows welcome screen background for all user accounts.",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(88, 88, 88),
                AutoSize = false,
                Width = 740,
                Height = 30,
                Location = new Point(20, 60),
                TextAlign = ContentAlignment.MiddleCenter
            };
            mainPanel.Controls.Add(instructionLabel);

            // File path panel
            Panel filePanel = new Panel
            {
                Width = 740,
                Height = 60,
                Location = new Point(20, 100),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White
            };
            filePanel.Paint += (s, e) => {
                using (Pen pen = new Pen(Color.FromArgb(218, 218, 218)))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, filePanel.Width - 1, filePanel.Height - 1);
                }
            };
            mainPanel.Controls.Add(filePanel);

            // File path label
            fileLabel = new Label
            {
                Text = "No file selected",
                AutoSize = false,
                Width = 720,
                Height = 40,
                Location = new Point(10, 10),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(88, 88, 88)
            };
            filePanel.Controls.Add(fileLabel);

            // Preview panel
            Panel previewPanel = new Panel
            {
                Width = 740,
                Height = 240,
                Location = new Point(20, 170),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White
            };
            previewPanel.Paint += (s, e) => {
                using (Pen pen = new Pen(Color.FromArgb(218, 218, 218)))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, previewPanel.Width - 1, previewPanel.Height - 1);
                }
            };
            mainPanel.Controls.Add(previewPanel);

            // Preview box
            previewBox = new PictureBox
            {
                Width = 720,
                Height = 220,
                Location = new Point(10, 10),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White
            };
            previewPanel.Controls.Add(previewBox);

            // Button panel for centered alignment
            Panel buttonPanel = new Panel
            {
                Width = 740,
                Height = 80,
                Location = new Point(20, 420),
                BackColor = backgroundColor
            };
            mainPanel.Controls.Add(buttonPanel);

            // Browse button
            Button browseButton = CreateStyledButton("Browse for Image", new Point(245, 0));
            browseButton.Click += SelectImage_Click;
            buttonPanel.Controls.Add(browseButton);

            // Undo button
            Button undoButton = CreateStyledButton("Revert to Original Settings", new Point(245, 40));
            undoButton.Click += (s, e) => UndoChanges();
            buttonPanel.Controls.Add(undoButton);

            // Create bottom panel for debug checkbox and version
            Panel bottomPanel = new Panel
            {
                Height = 30,
                Dock = DockStyle.Bottom,
                BackColor = backgroundColor,
                Padding = new Padding(20, 0, 20, 0)
            };
            this.Controls.Add(bottomPanel);

            // Add debug checkbox
            debugCheckBox = new CheckBox
            {
                Text = "Show Registry Debug Info",
                Location = new Point(20, 5),
                AutoSize = true,
                Checked = false
            };
            bottomPanel.Controls.Add(debugCheckBox);

            // Version label
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            string versionText = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
            versionLabel = new Label
            {
                Text = $"v{versionText}",
                AutoSize = true,
                Location = new Point(bottomPanel.Width - 80, 5),
                ForeColor = Color.Gray
            };
            bottomPanel.Controls.Add(versionLabel);

            // Handle form resize to update version label position
            bottomPanel.Resize += (s, e) =>
            {
                if (versionLabel != null)
                {
                    versionLabel.Location = new Point(bottomPanel.Width - 80, 5);
                }
            };
        }

        private Button CreateStyledButton(string text, Point location)
        {
            Button button = new Button
            {
                Text = text,
                Width = 250,
                Height = 35,
                Location = location,
                FlatStyle = FlatStyle.Flat,
                BackColor = primaryColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Cursor = Cursors.Hand
            };

            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = buttonHoverColor;

            return button;
        }

        private void CheckAdminRights()
        {
            bool isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);
            if (!isAdmin)
            {
                MessageBox.Show("This application requires administrative privileges to modify registry settings.",
                    "Admin Rights Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Application.Exit();
            }
        }

        private void CreatePlaceholderImage()
        {
            int width = 200;
            int height = 150;
            placeholderImage = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(placeholderImage))
            {
                // Fill background with a light gray
                g.Clear(Color.FromArgb(250, 250, 250));

                // Draw a photo icon
                using (Pen pen = new Pen(Color.FromArgb(200, 200, 200), 2))
                {
                    // Draw a rectangle for the photo frame
                    g.DrawRectangle(pen, width/4, height/4, width/2, height/2);

                    // Draw a mountain scene
                    Point[] mountains = {
                        new Point(width/4, 3*height/4),          // Left bottom
                        new Point(3*width/8, height/2),          // Small peak
                        new Point(width/2, 2*height/3),          // Valley
                        new Point(5*width/8, 5*height/12),       // High peak
                        new Point(3*width/4, 3*height/4)         // Right bottom
                    };
                    g.DrawLines(pen, mountains);

                    // Draw a sun
                    g.DrawEllipse(pen, 5*width/8, height/3, width/12, width/12);
                }

                // Add text
                using (Font font = new Font("Segoe UI", 9))
                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    g.DrawString("Select an image to preview", font, Brushes.Gray, 
                        new RectangleF(0, height - 40, width, 30), sf);
                }
            }
        }

        private void ShowPlaceholder()
        {
            if (previewBox.Image != null && previewBox.Image != placeholderImage)
            {
                previewBox.Image.Dispose();
            }
            previewBox.Image = placeholderImage;
        }

        private void UpdatePreview(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath))
                {
                    ShowPlaceholder();
                    return;
                }

                if (previewBox.Image != null && previewBox.Image != placeholderImage)
                {
                    previewBox.Image.Dispose();
                }

                using (var img = Image.FromFile(imagePath))
                {
                    previewBox.Image = new Bitmap(img);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading image preview: {ex.Message}",
                    "Preview Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                ShowPlaceholder();
            }
        }

        private void SelectImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All Files|*.*";
                dialog.Title = "Select Lock Screen Image";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    selectedImagePath = dialog.FileName;
                    fileLabel.Text = selectedImagePath;
                    UpdatePreview(selectedImagePath);
                }
            }
        }

        private void CheckRegistryValues(string message)
        {
            if (!debugCheckBox.Checked) return;

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\PersonalizationCSP"))
                {
                    string status = "Key not found";
                    string path = "Key not found";
                    string url = "Key not found";

                    if (key != null)
                    {
                        status = key.GetValue("LockScreenImageStatus")?.ToString() ?? "Value not found";
                        path = key.GetValue("LockScreenImagePath")?.ToString() ?? "Value not found";
                        url = key.GetValue("LockScreenImageUrl")?.ToString() ?? "Value not found";
                    }

                    MessageBox.Show(
                        $"Registry Check - {message}:\n" +
                        $"Status: {status}\n" +
                        $"Path: {path}\n" +
                        $"URL: {url}",
                        $"Registry State - {message}",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error checking registry - {message}:\n{ex.Message}",
                    "Registry Check Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void SetLockScreen()
        {
            if (string.IsNullOrEmpty(selectedImagePath))
            {
                MessageBox.Show(
                    "Please select an image file first.",
                    "No Image Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                const string cspKeyPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\PersonalizationCSP";
                const string policyKeyPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Personalization";

                CheckRegistryValues("Before Any Changes");

                try
                {
                    // Create the PersonalizationCSP key directly without deleting first
                    Registry.SetValue(cspKeyPath, "LockScreenImageStatus", 1, RegistryValueKind.DWord);
                    CheckRegistryValues("After Setting Status");

                    Registry.SetValue(cspKeyPath, "LockScreenImagePath", selectedImagePath, RegistryValueKind.String);
                    CheckRegistryValues("After Setting Path");

                    Registry.SetValue(cspKeyPath, "LockScreenImageUrl", selectedImagePath, RegistryValueKind.String);
                    CheckRegistryValues("After Setting URL");

                    // Run reg query to verify from command line
                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    process.StartInfo.FileName = "reg";
                    process.StartInfo.Arguments = "query \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\PersonalizationCSP\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    string regOutput = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    MessageBox.Show(
                        $"Reg Query Output:\n{regOutput}",
                        "Debug - Registry Query",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    // Set policy values
                    Registry.SetValue(policyKeyPath, "LockScreenImage", selectedImagePath, RegistryValueKind.String);
                    Registry.SetValue(policyKeyPath, "NoChangingLockScreen", 1, RegistryValueKind.DWord);

                    CheckRegistryValues("After Setting Policy Values");

                    MessageBox.Show(
                        "Welcome screen background has been set successfully!",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    // Final check after everything is done
                    CheckRegistryValues("Final Check");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error setting registry values:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                        "Registry Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error setting welcome screen background:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void UndoChanges()
        {
            try
            {
                // Delete PersonalizationCSP key
                try
                {
                    Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\Microsoft\Windows\CurrentVersion\PersonalizationCSP", false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error removing PersonalizationCSP key: {ex.Message}",
                        "Registry Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                // Delete policy values
                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Personalization", true))
                    {
                        if (key != null)
                        {
                            key.DeleteValue("LockScreenImage", false);
                            key.DeleteValue("NoChangingLockScreen", false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error removing policy values: {ex.Message}",
                        "Registry Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                // Clear the UI
                selectedImagePath = null;
                fileLabel.Text = "No file selected";
                ShowPlaceholder();

                MessageBox.Show(
                    "Welcome screen settings have been reverted to default.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error reverting settings: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void LoadCurrentImage()
        {
            try
            {
                string currentImagePath = null;

                // Try PersonalizationCSP path first
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\PersonalizationCSP"))
                {
                    if (key != null)
                    {
                        currentImagePath = key.GetValue("LockScreenImagePath") as string;
                    }
                }

                // If not found, try Personalization path
                if (string.IsNullOrEmpty(currentImagePath))
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Personalization"))
                    {
                        if (key != null)
                        {
                            currentImagePath = key.GetValue("LockScreenImage") as string;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(currentImagePath) && File.Exists(currentImagePath))
                {
                    selectedImagePath = currentImagePath;
                    fileLabel.Text = currentImagePath;
                    UpdatePreview(currentImagePath);
                }
            }
            catch (Exception)
            {
                // Silently fail if we can't read the current image
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (previewBox.Image != null)
            {
                previewBox.Image.Dispose();
            }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
