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
        private string selectedImagePath = null;
        private Color primaryColor = Color.FromArgb(0, 120, 212);  // Windows blue
        private Color backgroundColor = Color.FromArgb(243, 243, 243);
        private Color buttonHoverColor = Color.FromArgb(0, 102, 184);
        private PictureBox previewBox;
        private Label fileLabel;
        private Label versionLabel;

        public MainForm()
        {
            InitializeComponents();
            CheckAdminRights();
            LoadCurrentImage();
        }

        private void InitializeComponents()
        {
            // Set form properties
            this.Text = "Windows 11 Welcome Screen Customizer";
            this.Size = new Size(800, 600);
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
                Text = "Select an image file to set as your Windows welcome screen background.",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(88, 88, 88),
                AutoSize = false,
                Width = 740,
                Height = 30,
                Location = new Point(20, 60),
                TextAlign = ContentAlignment.MiddleCenter
            };
            mainPanel.Controls.Add(instructionLabel);

            // Version label
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            string versionText = $"{version.Major}.{version.Minor}.{version.Build}";
            versionLabel = new Label
            {
                Text = $"v{versionText}",
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(mainPanel.Width - 80, mainPanel.Height - 25)
            };
            mainPanel.Controls.Add(versionLabel);
            mainPanel.Resize += (s, e) => 
            {
                // Keep version label in bottom-right corner when form is resized
                versionLabel.Location = new Point(mainPanel.Width - 80, mainPanel.Height - 25);
            };

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
            browseButton.Click += (s, e) => BrowseFile(fileLabel);
            buttonPanel.Controls.Add(browseButton);

            // Undo button
            Button undoButton = CreateStyledButton("Revert to Original Settings", new Point(245, 40));
            undoButton.Click += (s, e) => UndoChanges();
            buttonPanel.Controls.Add(undoButton);
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
            button.MouseEnter += (s, e) => button.BackColor = buttonHoverColor;
            button.MouseLeave += (s, e) => button.BackColor = primaryColor;

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

        private void UpdatePreview(string imagePath)
        {
            try
            {
                if (previewBox.Image != null)
                {
                    previewBox.Image.Dispose();
                    previewBox.Image = null;
                }

                if (!string.IsNullOrEmpty(imagePath))
                {
                    using (var img = Image.FromFile(imagePath))
                    {
                        // Create a new bitmap to avoid file locking
                        previewBox.Image = new Bitmap(img);
                    }
                }
            }
            catch (Exception)
            {
                // If there's an error loading the image, clear the preview
                if (previewBox.Image != null)
                {
                    previewBox.Image.Dispose();
                    previewBox.Image = null;
                }
            }
        }

        private void BrowseFile(Label fileLabel)
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
                    SetLockScreen(); // Automatically set the image after selection
                }
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
                // First, set the image using PersonalizationCSP
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\PersonalizationCSP"))
                {
                    if (key != null)
                    {
                        key.SetValue("LockScreenImageStatus", 1);
                        key.SetValue("LockScreenImagePath", selectedImagePath);
                        key.SetValue("LockScreenImageUrl", selectedImagePath);
                    }
                }

                // Then, set it using the Personalization key and prevent changes
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Personalization"))
                {
                    if (key != null)
                    {
                        key.SetValue("LockScreenImage", selectedImagePath);
                        key.SetValue("NoChangingLockScreen", 1);
                    }
                }

                MessageBox.Show(
                    "Welcome screen background has been set successfully!",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Error setting welcome screen background: {0}", ex.Message),
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void UndoChanges()
        {
            try
            {
                // Remove Personalization policy values
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Policies\Microsoft\Windows\Personalization", true))
                {
                    if (key != null)
                    {
                        try { key.DeleteValue("LockScreenImage"); } catch { }
                        try { key.DeleteValue("NoChangingLockScreen"); } catch { }
                    }
                }

                // Remove PersonalizationCSP values
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\PersonalizationCSP", true))
                {
                    if (key != null)
                    {
                        // Delete all values
                        try { key.DeleteValue("LockScreenImageStatus"); } catch { }
                        try { key.DeleteValue("LockScreenImagePath"); } catch { }
                        try { key.DeleteValue("LockScreenImageUrl"); } catch { }

                        // If the key is empty after deleting values, delete the entire key
                        if (key.GetValueNames().Length == 0 && key.GetSubKeyNames().Length == 0)
                        {
                            Registry.LocalMachine.DeleteSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\PersonalizationCSP", false);
                        }
                    }
                }

                // Clear the UI
                selectedImagePath = null;
                fileLabel.Text = "No file selected";
                if (previewBox.Image != null)
                {
                    previewBox.Image.Dispose();
                    previewBox.Image = null;
                }

                MessageBox.Show("All lock screen registry changes have been removed.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error removing registry changes: {0}", ex.Message),
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
