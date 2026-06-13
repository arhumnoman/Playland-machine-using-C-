using System;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

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
    private readonly Label statusLabel;
    private string? detectedPortName;
    private readonly Label resultLabel;
    private readonly Label rawInputLabel;
    private readonly Label parsedLabel;
    private SerialPort? serialPort;

    public MainForm()
    {
        Text = "Playland Boxer";
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.White;

        var headerLabel = new Label
        {
            Text = "Serial Input: numeric values only from COM port; auto-enter first value",
            ForeColor = Color.Black,
            Font = new Font(Font.FontFamily, 18, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 20)
        };

        box1Label = new Label { Text = "Highest score:", Location = new Point(40, 100), AutoSize = true, ForeColor = Color.Black };
        box1Input = new NumericUpDown { Location = new Point(180, 98), Width = 220, Minimum = 0, Maximum = 999, Value = 0, ReadOnly = true, TabStop = false, BackColor = Color.White, ForeColor = Color.Black };

        box2Label = new Label { Text = "2nd high score:", Location = new Point(40, 160), AutoSize = true, ForeColor = Color.Black };
        box2Input = new NumericUpDown { Location = new Point(180, 158), Width = 220, Minimum = 0, Maximum = 999, Value = 0, ReadOnly = true, TabStop = false, BackColor = Color.White, ForeColor = Color.Black };

        box3Label = new Label { Text = "3rd high score:", Location = new Point(40, 220), AutoSize = true, ForeColor = Color.Black };
        box3Input = new NumericUpDown { Location = new Point(180, 218), Width = 220, Minimum = 0, Maximum = 999, Value = 0, ReadOnly = true, TabStop = false, BackColor = Color.White, ForeColor = Color.Black };

        manualLabel = new Label { Text = "Manual Test Value:", Location = new Point(40, 280), AutoSize = true, ForeColor = Color.Black };
        manualInput = new NumericUpDown { Location = new Point(260, 278), Width = 140, Minimum = 0, Maximum = 999, Value = 0, BackColor = Color.White, ForeColor = Color.Black };

        enterNumberButton = new Button { Text = "Enter Number", Location = new Point(40, 340), Width = 180 };
        enterNumberButton.Click += EnterNumberButton_Click;

        connectButton = new Button { Text = "Auto Connect", Location = new Point(40, 420), Width = 160 };
        connectButton.Click += ConnectButton_Click;

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
        Controls.Add(statusLabel);
        Controls.Add(resultLabel);
        Controls.Add(rawInputLabel);
        Controls.Add(parsedLabel);
    }

    private async void ConnectButton_Click(object? sender, EventArgs e)
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            DisconnectSerialPort();
            return;
        }

        connectButton.Enabled = false;
        UpdateStatus("Scanning COM ports...", Color.Black);

        var portNames = SerialPort.GetPortNames();
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

        UpdateStatus("Could not auto-detect an ESP connection on any COM port.", Color.Red);
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
            while (sw.ElapsedMilliseconds < 4500)
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
                            if (Regex.IsMatch(trimmed, "^\\d{3}$"))
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
            if (!string.IsNullOrEmpty(line))
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

        var matches = Regex.Matches(data, @"[+-]?\d+(\.\d+)?");
        if (matches.Count == 0)
        {
            parsedLabel.Text = "Parsed values: none";
            UpdateStatus("No numeric value found in serial input.", Color.Orange);
            return;
        }

        var values = matches.Cast<Match>()
            .Select(m => decimal.Parse(m.Value))
            .ToArray();

        parsedLabel.Text = $"Parsed values: {string.Join(", ", values)}";

        // Automatically set the first numeric raw value into the manual entry field.
        ProcessEnteredNumber(values[0]);
        UpdateStatus("Serial data received and entered to manual value.", Color.LightGreen);
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
        base.OnFormClosing(e);
    }
}
