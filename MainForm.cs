using System;
using System.Drawing;
using System.Windows.Forms;

namespace PlaylandBoxer;

public class MainForm : Form
{
    private readonly NumericUpDown box1Input;
    private readonly NumericUpDown box2Input;
    private readonly NumericUpDown box3Input;
    private readonly Label box1Label;
    private readonly Label box2Label;
    private readonly Label box3Label;
    private readonly Button showValuesButton;

    public MainForm()
    {
        Text = "Playland Boxer";
        ClientSize = new Size(320, 220);
        StartPosition = FormStartPosition.CenterScreen;

        box1Label = new Label { Text = "Box 1:", Location = new Point(20, 20), AutoSize = true };
        box1Input = new NumericUpDown { Location = new Point(100, 18), Width = 180, Minimum = 0, Maximum = 999, Value = 0 };

        box2Label = new Label { Text = "Box 2:", Location = new Point(20, 70), AutoSize = true };
        box2Input = new NumericUpDown { Location = new Point(100, 68), Width = 180, Minimum = 0, Maximum = 999, Value = 0 };

        box3Label = new Label { Text = "Box 3:", Location = new Point(20, 120), AutoSize = true };
        box3Input = new NumericUpDown { Location = new Point(100, 118), Width = 180, Minimum = 0, Maximum = 999, Value = 0 };

        showValuesButton = new Button { Text = "Show values", Location = new Point(100, 170), Width = 180 };
        showValuesButton.Click += ShowValuesButton_Click;

        Controls.Add(box1Label);
        Controls.Add(box1Input);
        Controls.Add(box2Label);
        Controls.Add(box2Input);
        Controls.Add(box3Label);
        Controls.Add(box3Input);
        Controls.Add(showValuesButton);
    }

    private void ShowValuesButton_Click(object? sender, EventArgs e)
    {
        MessageBox.Show($"Box 1: {box1Input.Value}\nBox 2: {box2Input.Value}\nBox 3: {box3Input.Value}", "Values", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
