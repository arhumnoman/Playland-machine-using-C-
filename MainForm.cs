using System;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Web.WebView2.WinForms;

namespace PlaylandBoxer;

public class MainForm : Form
{
    private readonly NumericUpDown box1Input;
    private readonly NumericUpDown box2Input;
    private readonly NumericUpDown box3Input;
    private readonly NumericUpDown manualInput;
    private readonly Label box1Label;
    private readonly Label box2Label;
    private readonly Label box3Label;
    private readonly Label manualLabel;
    private readonly Button enterNumberButton;
    private readonly Button connectButton;
    private readonly Button adminButton;
    private readonly Label statusLabel;
    private string espStartupKey = "123456789";
    private const string AdminUserId = "arhum";
    private const string AdminPassword = "1906";
    private string? detectedPortName;
    private readonly Label resultLabel;
    private readonly Label rawInputLabel;
    private readonly Label parsedLabel;
    private SerialPort? serialPort;
    private WebView2? webView;
    private Panel? webViewPanel;
    private Label? errorLabel;

    public MainForm()
    {
        Text = "Playland Boxer";
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.Black;

        // Initialize video player
        InitializeVideoPlayer();

        // Handle the Shown event to complete video setup
        this.Shown += MainForm_Shown;

        var headerLabel = new Label
        {
            Text = "Serial Input: numeric values only from COM port; auto-enter first value",
            ForeColor = Color.Black,
            Font = new Font(Font.FontFamily, 18, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 20)
        };

        // Use solid backgrounds for controls that don't support transparent colors
        box1Label = new Label { Text = "Highest score:", Location = new Point(40, 100), AutoSize = true, ForeColor = Color.Black, BackColor = Color.White };
        box1Input = new NumericUpDown { Location = new Point(180, 98), Width = 220, Minimum = 0, Maximum = 999, Value = 0, ReadOnly = true, TabStop = false, BackColor = Color.White, ForeColor = Color.Black };

        box2Label = new Label { Text = "2nd high score:", Location = new Point(40, 160), AutoSize = true, ForeColor = Color.Black, BackColor = Color.White };
        box2Input = new NumericUpDown { Location = new Point(180, 158), Width = 220, Minimum = 0, Maximum = 999, Value = 0, ReadOnly = true, TabStop = false, BackColor = Color.White, ForeColor = Color.Black };

        box3Label = new Label { Text = "3rd high score:", Location = new Point(40, 220), AutoSize = true, ForeColor = Color.Black, BackColor = Color.White };
        box3Input = new NumericUpDown { Location = new Point(180, 218), Width = 220, Minimum = 0, Maximum = 999, Value = 0, ReadOnly = true, TabStop = false, BackColor = Color.White, ForeColor = Color.Black };

        manualLabel = new Label { Text = "Manual Test Value:", Location = new Point(40, 280), AutoSize = true, ForeColor = Color.Black, BackColor = Color.White };
        manualInput = new NumericUpDown { Location = new Point(260, 278), Width = 140, Minimum = 0, Maximum = 999, Value = 0, BackColor = Color.White, ForeColor = Color.Black };

        enterNumberButton = new Button { Text = "Enter Number", Location = new Point(40, 340), Width = 180 };
        enterNumberButton.Click += EnterNumberButton_Click;

        connectButton = new Button { Text = "Auto Connect", Location = new Point(40, 420), Width = 160 };
        connectButton.Click += ConnectButton_Click;

        adminButton = new Button { Text = "Admin", Location = new Point(220, 420), Width = 120 };
        adminButton.Click += AdminButton_Click;

        statusLabel = new Label
        {
            Text = "Status: disconnected",
            Location = new Point(40, 470),
            AutoSize = true,
            ForeColor = Color.Black
        };

        resultLabel = new Label
        {
            Text = "Sorted output: 0, 0, 0",
            Location = new Point(40, 520),
            AutoSize = true,
            ForeColor = Color.Black,
            Font = new Font(Font.FontFamily, 14, FontStyle.Bold)
        };

        rawInputLabel = new Label
        {
            Text = "Raw serial: none",
            Location = new Point(40, 560),
            AutoSize = true,
            ForeColor = Color.Black,
            Font = new Font(Font.FontFamily, 10, FontStyle.Regular)
        };

        parsedLabel = new Label
        {
            Text = "Parsed values: none",
            Location = new Point(40, 590),
            AutoSize = true,
            ForeColor = Color.Black,
            Font = new Font(Font.FontFamily, 10, FontStyle.Regular)
        };

        Controls.Add(headerLabel);
        Controls.Add(box1Label);
        Controls.Add(box1Input);
        Controls.Add(box2Label);
        Controls.Add(box2Input);
        Controls.Add(box3Label);
        Controls.Add(box3Input);
        Controls.Add(manualLabel);
        Controls.Add(manualInput);
        Controls.Add(enterNumberButton);
        Controls.Add(connectButton);
        Controls.Add(adminButton);
        Controls.Add(statusLabel);
        Controls.Add(resultLabel);
        Controls.Add(rawInputLabel);
        Controls.Add(parsedLabel);

        // Make text white and hide numeric boxes (remove visible "boxes")
        headerLabel.ForeColor = Color.White;

        box1Label.ForeColor = Color.White;
        box1Label.BackColor = Color.Transparent;
        box1Input.Visible = false;

        box2Label.ForeColor = Color.White;
        box2Label.BackColor = Color.Transparent;
        box2Input.Visible = false;

        box3Label.ForeColor = Color.White;
        box3Label.BackColor = Color.Transparent;
        box3Input.Visible = false;

        manualLabel.ForeColor = Color.White;
        manualLabel.BackColor = Color.Transparent;
        manualInput.Visible = false;

        statusLabel.ForeColor = Color.White;
        resultLabel.ForeColor = Color.White;
        rawInputLabel.ForeColor = Color.White;
        parsedLabel.ForeColor = Color.White;

        enterNumberButton.ForeColor = Color.White;
        connectButton.ForeColor = Color.White;
        adminButton.ForeColor = Color.White;

        // Bring all UI controls to front so they appear above the video background
        BringToFront();
        headerLabel.BringToFront();
        box1Label.BringToFront();
        box1Input.BringToFront();
        box2Label.BringToFront();
        box2Input.BringToFront();
        box3Label.BringToFront();
        box3Input.BringToFront();
        manualLabel.BringToFront();
        manualInput.BringToFront();
        enterNumberButton.BringToFront();
        connectButton.BringToFront();
        adminButton.BringToFront();
        statusLabel.BringToFront();
        resultLabel.BringToFront();
        rawInputLabel.BringToFront();
        parsedLabel.BringToFront();
    }

    private void ShowError(string message)
    {
        if (errorLabel == null)
        {
            errorLabel = new Label
            {
                Text = message,
                ForeColor = Color.Blue,
                Font = new Font(Font.FontFamily, 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(40, 640),
                BackColor = Color.Transparent
            };
            Controls.Add(errorLabel);
            errorLabel.BringToFront();
        }
        else
        {
            errorLabel.Text = message;
            errorLabel.Visible = true;
            errorLabel.BringToFront();
        }

        Console.WriteLine($"Error shown: {message}");
    }

    private void InitializeVideoPlayer()
    {
        try
        {
            // Create a panel for the webview
            webViewPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                Name = "webViewPanel"
            };
            Controls.Add(webViewPanel);
            webViewPanel.SendToBack();

            // Initialize WebView2
            webView = new WebView2
            {
                Dock = DockStyle.Fill,
                Name = "webView"
            };
            webViewPanel.Controls.Add(webView);

            // Initialize WebView2 asynchronously
            var initTask = webView.EnsureCoreWebView2Async();
            initTask.ContinueWith(async _ =>
            {
                try
                {
                    // Wait a moment for the core to fully initialize
                    await Task.Delay(500);
                    
                    if (webView?.CoreWebView2 != null)
                    {
                        // Allow access to local files
                        webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                        webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                        
                        Debug.WriteLine("WebView2 CoreWebView2 initialized successfully");
                        LoadVideoIntoWebView();
                    }
                    else
                    {
                        Debug.WriteLine("WebView2 CoreWebView2 is still null after EnsureCoreWebView2Async");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in async initialization: {ex.Message}");
                    Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Video player initialization failed: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private void LoadVideoIntoWebView()
    {
        try
        {
            Console.WriteLine("LoadVideoIntoWebView called");

                // Find the video folder - it should be in the project root, not in bin
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                Console.WriteLine($"Base directory: {baseDir}");

                // Try multiple possible locations for the video folder
                string[] possiblePaths = new string[]
                {
                    Path.Combine(baseDir, "video"), // If running from bin/Debug/net8.0-windows
                    Path.Combine(baseDir, "..", "..", "..", "video"), // Navigate up from bin/Debug/net8.0-windows to project root
                    Path.Combine(baseDir, "..\\..\\..\\video"), // Windows path separator
                    @"e:\GIT HUB Projects\Playland P1\Playland-machine-using-C-\video" // Absolute path
                };

                string? videoFolder = null;
                foreach (var path in possiblePaths)
                {
                    string fullPath = Path.GetFullPath(path);
                    Console.WriteLine($"Checking: {fullPath}");
                    if (Directory.Exists(fullPath))
                    {
                        videoFolder = fullPath;
                        Console.WriteLine($"Found video folder at: {videoFolder}");
                        break;
                    }
                }

                if (string.IsNullOrEmpty(videoFolder))
                {
                    string errMsg = "Error: Video folder not found in any expected location";
                    Console.WriteLine(errMsg);
                    ShowError(errMsg);
                    return;
                }

                string videoFilePath = Path.Combine(videoFolder, "1.mp4");
                Console.WriteLine($"Looking for video at: {videoFilePath}");

                if (!File.Exists(videoFilePath))
                {
                    string errMsg = "Error: Video file not found at " + videoFilePath;
                    Console.WriteLine(errMsg);
                    ShowError(errMsg);
                    return;
                }

                Console.WriteLine($"Found video file: {videoFilePath}");

            // Create an HTML file to serve the video
                string htmlFilePath = Path.Combine(videoFolder, "player.html");
            Console.WriteLine($"Creating HTML file at: {htmlFilePath}");

                // Create simple HTML content
                string htmlContent = @"<!DOCTYPE html
<html>
<head>
    <style>
        body { margin: 0; padding: 0; background: #000; }
        video { width: 100%; height: 100%; object-fit: cover; }
    </style>
</head>
<body>
    <video autoplay muted loop>
        <source src=""1.mp4"" type=""video/mp4"">
    </video>
</body>
</html>";

            // Write the HTML file
            File.WriteAllText(htmlFilePath, htmlContent);
            Console.WriteLine("HTML file created");

            // Navigate to the HTML file
            if (webView?.CoreWebView2 != null)
            {
                string fileUrl = new Uri(htmlFilePath).AbsoluteUri;
                Console.WriteLine($"Navigating to: {fileUrl}");
                webView.CoreWebView2.Navigate(fileUrl);
                Console.WriteLine("Navigation complete");
            }
            else
            {
                string errMsg = "Error: WebView2 not initialized";
                Console.WriteLine(errMsg);
                ShowError(errMsg);
            }
        }
        catch (Exception ex)
        {
            string errMsg = $"Error: Video playback failed - {ex.Message}";
            Console.WriteLine(errMsg);
            Console.WriteLine($"Stack: {ex.StackTrace}");
            ShowError(errMsg);
        }
    }

    private void StartVideoPlayer(string videoFile)
    {
        // This method is no longer used - video is handled by WebView2
    }

    private string? FindExecutable(string exeName)
    {
        // This method is no longer used - video is handled by WebView2
        return null;
    }

    private async void MainForm_Shown(object? sender, EventArgs e)
    {
        try
        {
            Console.WriteLine("MainForm_Shown called");

            if (webView == null)
            {
                Console.WriteLine("webView is null");
                ShowError("Error: WebView initialization failed");
                return;
            }

            Console.WriteLine("Calling EnsureCoreWebView2Async");
            await webView.EnsureCoreWebView2Async();
            
            Console.WriteLine("Waiting 1 second...");
            await Task.Delay(1000);

            if (webView.CoreWebView2 != null)
            {
                Console.WriteLine("CoreWebView2 is ready");
                webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                LoadVideoIntoWebView();
            }
            else
            {
                Console.WriteLine("CoreWebView2 is still null");
                ShowError("Error: WebView2 engine not available");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in MainForm_Shown: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            ShowError($"Error: {ex.Message}");
        }
    }

    private async void ConnectButton_Click(object? sender, EventArgs e)
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            DisconnectSerialPort();
            return;
        }

        connectButton.Enabled = false;
        UpdateStatus("Scanning COM ports for ESP startup key...", Color.Black);

        var portNames = SerialPort.GetPortNames().Distinct().OrderBy(n => n).ToArray();
        if (portNames.Length == 0)
        {
            UpdateStatus("No COM ports found.", Color.Orange);
            connectButton.Enabled = true;
            return;
        }

        foreach (var portName in portNames)
        {
            UpdateStatus($"Testing {portName}...", Color.Black);
            if (await Task.Run(() => TryConnectToEspPort(portName)))
            {
                detectedPortName = portName;
                connectButton.Text = "Disconnect";
                UpdateStatus($"Connected to ESP on {portName}.", Color.LightGreen);
                connectButton.Enabled = true;
                return;
            }
        }

        UpdateStatus("Wrong ESP. No port sent the expected startup key.", Color.Red);
        connectButton.Enabled = true;
    }

    private void EnterNumberButton_Click(object? sender, EventArgs e)
    {
        var input = ShowInputDialog("Input a number:", "Enter a number");
        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        if (!decimal.TryParse(input.Trim(), out decimal parsed))
        {
            MessageBox.Show("Please enter a valid numeric value.", "Invalid input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        ProcessEnteredNumber(parsed);
    }

    private void ProcessEnteredNumber(decimal parsed)
    {
        manualInput.Value = ClampValue(parsed);

        var current = new[] { box1Input.Value, box2Input.Value, box3Input.Value, manualInput.Value }
            .OrderByDescending(v => v)
            .Take(3)
            .ToArray();

        box1Input.Value = ClampValue(current[0]);
        box2Input.Value = ClampValue(current[1]);
        box3Input.Value = ClampValue(current[2]);

        var ascending = current.OrderBy(v => v).ToArray();
        parsedLabel.Text = $"Parsed values: {string.Join(", ", current)}";
        UpdateStatus("Entered number processed.", Color.LightGreen);
        UpdateOutput(ascending);
    }

    private static string? ShowInputDialog(string text, string caption)
    {
        using var prompt = new Form
        {
            Width = 360,
            Height = 160,
            Text = caption,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false
        };

        using var textBox = new TextBox { Left = 20, Top = 20, Width = 300 };
        using var okButton = new Button { Text = "OK", Left = 160, Width = 80, Top = 60, DialogResult = DialogResult.OK };
        using var cancelButton = new Button { Text = "Cancel", Left = 250, Width = 80, Top = 60, DialogResult = DialogResult.Cancel };

        prompt.Controls.Add(textBox);
        prompt.Controls.Add(okButton);
        prompt.Controls.Add(cancelButton);
        prompt.AcceptButton = okButton;
        prompt.CancelButton = cancelButton;

        return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : null;
    }

    private void AdminButton_Click(object? sender, EventArgs e)
    {
        if (!ShowAdminLoginDialog())
        {
            return;
        }

        using var keyForm = new Form
        {
            Width = 420,
            Height = 200,
            Text = "ESP Startup Key",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var keyLabel = new Label { Text = "ESP startup key:", Left = 20, Top = 20, AutoSize = true };
        var keyText = new TextBox { Left = 120, Top = 18, Width = 250, Text = espStartupKey };

        var okButton = new Button { Text = "Save", Left = 120, Width = 100, Top = 70, DialogResult = DialogResult.OK };
        var cancelButton = new Button { Text = "Cancel", Left = 230, Width = 100, Top = 70, DialogResult = DialogResult.Cancel };

        keyForm.Controls.Add(keyLabel);
        keyForm.Controls.Add(keyText);
        keyForm.Controls.Add(okButton);
        keyForm.Controls.Add(cancelButton);
        keyForm.AcceptButton = okButton;
        keyForm.CancelButton = cancelButton;

        if (keyForm.ShowDialog() == DialogResult.OK)
        {
            if (string.IsNullOrWhiteSpace(keyText.Text))
            {
                MessageBox.Show("ESP startup key cannot be empty.", "Invalid key", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            espStartupKey = keyText.Text.Trim();
            UpdateStatus("ESP startup key updated.", Color.LightGreen);
        }
    }

    private bool ShowAdminLoginDialog()
    {
        using var loginForm = new Form
        {
            Width = 420,
            Height = 180,
            Text = "Admin Login",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var userLabel = new Label { Text = "User ID:", Left = 20, Top = 20, AutoSize = true };
        var userText = new TextBox { Left = 120, Top = 18, Width = 250 };

        var passwordLabel = new Label { Text = "Password:", Left = 20, Top = 60, AutoSize = true };
        var passwordText = new TextBox { Left = 120, Top = 58, Width = 250, UseSystemPasswordChar = true };

        var okButton = new Button { Text = "Login", Left = 120, Width = 100, Top = 100, DialogResult = DialogResult.OK };
        var cancelButton = new Button { Text = "Cancel", Left = 230, Width = 100, Top = 100, DialogResult = DialogResult.Cancel };

        loginForm.Controls.Add(userLabel);
        loginForm.Controls.Add(userText);
        loginForm.Controls.Add(passwordLabel);
        loginForm.Controls.Add(passwordText);
        loginForm.Controls.Add(okButton);
        loginForm.Controls.Add(cancelButton);
        loginForm.AcceptButton = okButton;
        loginForm.CancelButton = cancelButton;

        while (loginForm.ShowDialog() == DialogResult.OK)
        {
            if (userText.Text == AdminUserId && passwordText.Text == AdminPassword)
            {
                return true;
            }

            MessageBox.Show("Invalid admin credentials.", "Access denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            userText.Clear();
            passwordText.Clear();
            userText.Focus();
        }

        return false;
    }

    private bool TryConnectToEspPort(string portName)
    {
        var candidate = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One)
        {
            NewLine = "\n",
            ReadTimeout = 500
        };

        try
        {
            candidate.Open();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 6000)
            {
                if (candidate.BytesToRead > 0)
                {
                    var data = candidate.ReadExisting();
                    if (!string.IsNullOrEmpty(data))
                    {
                        var lines = data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            var trimmed = line.Trim();
                            if (trimmed == espStartupKey)
                            {
                                candidate.DataReceived += SerialPort_DataReceived;
                                serialPort = candidate;
                                return true;
                            }
                        }
                    }
                }

                System.Threading.Thread.Sleep(100);
            }
        }
        catch
        {
            // ignore invalid or unreachable port
        }

        try
        {
            if (candidate.IsOpen)
            {
                candidate.Close();
            }
        }
        catch
        {
            // ignore cleanup exceptions
        }
        finally
        {
            if (serialPort != candidate)
            {
                candidate.Dispose();
            }
        }

        return false;
    }

    private void DisconnectSerialPort()
    {
        if (serialPort != null)
        {
            try
            {
                serialPort.DataReceived -= SerialPort_DataReceived;
                serialPort.Close();
            }
            catch { }
            finally
            {
                serialPort.Dispose();
                serialPort = null;
            }
        }

        connectButton.Text = "Connect";
        UpdateStatus("Disconnected", Color.LightGreen);
    }

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (serialPort == null)
        {
            return;
        }

        try
        {
            string? line = serialPort.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(line) && line != espStartupKey)
            {
                BeginInvoke(() => UpdateValuesFromSerial(line));
            }
        }
        catch
        {
            // ignore timeout or malformed data
        }
    }

    private void UpdateValuesFromSerial(string data)
    {
        rawInputLabel.Text = $"Raw serial: {data}";

        var matches = Regex.Matches(data, @"\b\d{3}\b");
        if (matches.Count == 0)
        {
            parsedLabel.Text = "Parsed values: none";
            UpdateStatus("No 3-digit score value found in serial input.", Color.Orange);
            return;
        }

        var values = matches.Cast<Match>()
            .Select(m => decimal.Parse(m.Value))
            .ToArray();

        parsedLabel.Text = $"Parsed values: {string.Join(", ", values)}";

        // Automatically set the first valid 3-digit score value into the manual entry field.
        ProcessEnteredNumber(values[0]);
        UpdateStatus("Serial 3-digit score received and entered.", Color.LightGreen);
    }

    private static decimal ClampValue(decimal value) => Math.Min(999, Math.Max(0, value));

    private void UpdateOutput()
    {
        var sortedValues = new[] { box1Input.Value, box2Input.Value, box3Input.Value }
            .OrderBy(v => v)
            .ToArray();

        resultLabel.Text = $"Ascending order: {sortedValues[0]}, {sortedValues[1]}, {sortedValues[2]}";
    }

    private void UpdateOutput(decimal[] ascending)
    {
        resultLabel.Text = $"Ascending order: {ascending[0]}, {ascending[1]}, {ascending[2]}";
    }

    private void UpdateStatus(string text, Color color)
    {
        statusLabel.Text = $"Status: {text}";
        statusLabel.ForeColor = color;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        DisconnectSerialPort();
        
        // Stop and dispose WebView2
        if (webView != null)
        {
            try
            {
                webView.Dispose();
            }
            catch { }
            webView = null;
        }
        
        if (webViewPanel != null)
        {
            try
            {
                webViewPanel.Dispose();
            }
            catch { }
            webViewPanel = null;
        }
        
        base.OnFormClosing(e);
    }
}
