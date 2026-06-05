using LiveChartsCore.SkiaSharpView.WinForms;

namespace TreadmillController;

public sealed partial class MainForm
{
    #region Controls

    private readonly TextBox _txtIp = new();
    private readonly Button _btnConnect = new();

    private readonly TextBox _txtSpeed = new();

    private readonly TextBox _txtIncline = new();

    private readonly RadioButton _rbKph = new();

    private readonly RadioButton _rbMph = new();

    private readonly Button _btnUpdate = new();

    private readonly Label _lblStatus = new();

    private readonly Label _lblConnection = new();

    private readonly Label _lblSpeed = new();

    private readonly Label _lblIncline = new();

    private readonly Label _lblWorkoutTime = new();

    private readonly Label _lblWorkoutDistance = new();

    // broadcast checkbox removed (FTMS/TCP mode)

    private readonly CheckBox _chkAutoDetectIp = new();

    private readonly CartesianChart _chart = new();

    //
    // Workout designer
    //
    private readonly ComboBox _cmbAxisMode = new();

    private readonly NumericUpDown _numXAxis = new();

    private readonly NumericUpDown _numMinIncline = new();

    private readonly NumericUpDown _numMaxIncline = new();

    private readonly Button _btnApplyChart = new();

    private readonly Button _btnWorkoutToggle = new();

    #endregion

    private void InitializeComponent()
    {
        SuspendLayout();

        AutoScaleMode = AutoScaleMode.Dpi;

        Font = new Font("Segoe UI", 10);

        Text = "Treadmill Controller (.NET 10)";

        MinimumSize = new Size(1400, 850);

        StartPosition =
            FormStartPosition.CenterScreen;

        DoubleBuffered = true;

        BuildLayout();

        ResumeLayout(false);
    }

    #region Layout

    private Control CreateLabeledTextbox(
        string labelText,
        TextBox textBox)
    {
        var panel = new TableLayoutPanel
        {
            ColumnCount = 2,
            Dock = DockStyle.Top,
            AutoSize = true
        };

        panel.ColumnStyles.Add(
            new ColumnStyle(SizeType.AutoSize));

        panel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                100));

        var label = new Label
        {
            Text = labelText,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 6, 0, 6)
        };

        textBox.Width = 80;

        textBox.Dock = DockStyle.Left;

        panel.Controls.Add(label, 0, 0);

        panel.Controls.Add(textBox, 1, 0);

        return panel;
    }

    private void BuildLayout()
    {
        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(12)
        };

        root.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Absolute,
                380));

        root.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                100));

        Controls.Add(root);

        //
        // LEFT PANEL
        //
        TableLayoutPanel left = new()
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            ColumnCount = 1,
            Padding = new Padding(10)
        };

        root.Controls.Add(left, 0, 0);

        //
        // RIGHT PANEL
        //
        Panel right = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        root.Controls.Add(right, 1, 0);

        //
        // IP
        //
        left.Controls.Add(
            CreateLabel("Treadmill IP"));

        _txtIp.Text = "192.168.1.255";

        // Reduce width to make room for Connect button on same line
        _txtIp.Width = 180;
        _txtIp.Dock = DockStyle.Left;

        // Put IP and connect button in a panel so they appear on the same line
        var ipPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = _txtIp.Height + 6
        };

        _btnConnect.Text = "Connect";
        _btnConnect.Width = 100;
        _btnConnect.Height = 30;
        _btnConnect.Left = _txtIp.Width + 10;
        _btnConnect.Top = 3;

        ipPanel.Controls.Add(_txtIp);
        ipPanel.Controls.Add(_btnConnect);

        left.Controls.Add(ipPanel);

        //
        // Broadcast
        //
        // Broadcast removed for FTMS/TCP mode

        //
        // Auto detect
        //
        _chkAutoDetectIp.Text =
            "Auto-detect Treadmill IP";

        _chkAutoDetectIp.Checked = true;

        _chkAutoDetectIp.AutoSize = true;

        left.Controls.Add(_chkAutoDetectIp);

        //
        // Units
        //
        GroupBox units = new()
        {
            Text = "Units",
            Dock = DockStyle.Top,
            Height = 60
        };

        FlowLayoutPanel unitsFlow = new()
        {
            Dock = DockStyle.None,
            AutoSize = true,
            AutoSizeMode =
                AutoSizeMode.GrowAndShrink,

            FlowDirection =
                FlowDirection.LeftToRight,

            WrapContents = false,

            Anchor = AnchorStyles.None
        };

        _rbKph.Text = "km/h";

        _rbKph.Checked = true;

        _rbKph.AutoSize = true;

        _rbKph.Margin = new Padding(5);

        _rbMph.Text = "mph";

        _rbMph.AutoSize = true;

        _rbMph.Margin = new Padding(5);

        unitsFlow.Controls.Add(_rbKph);

        unitsFlow.Controls.Add(_rbMph);

        units.Controls.Add(unitsFlow);

        //
        // Center after layout
        //
        units.Layout += (_, _) =>
        {
            unitsFlow.Left =
                (units.ClientSize.Width -
                 unitsFlow.Width) / 2;

            unitsFlow.Top =
                (units.ClientSize.Height -
                 unitsFlow.Height) / 2;
        };

        left.Controls.Add(units);

        //
        // Speed
        //
        _txtSpeed.Text = "5.0";

        left.Controls.Add(
            CreateLabeledTextbox(
                "Speed",
                _txtSpeed));

        //
        // Incline
        //
        _txtIncline.Text = "1.0";

        left.Controls.Add(
            CreateLabeledTextbox(
                "Incline",
                _txtIncline));

        //
        // Manual update button
        //
        _btnUpdate.Text =
            "UPDATE TREADMILL";

        _btnUpdate.Height = 55;

        _btnUpdate.Dock = DockStyle.Top;

        _btnUpdate.BackColor =
            Color.PaleGreen;

        _btnUpdate.Margin =
            new Padding(0, 15, 0, 15);

        left.Controls.Add(_btnUpdate);

        //
        // Workout setup
        //
        GroupBox workoutBox = new()
        {
            Text = "Workout Designer",
            Dock = DockStyle.Top,
            Height = 300
        };

        TableLayoutPanel workoutLayout = new()
        {
            Dock = DockStyle.Fill,
            RowCount = 10,
            ColumnCount = 2
        };

        workoutLayout.RowStyles.Clear();

        for (int i = 0; i < 10; i++)
        {
            workoutLayout.RowStyles.Add(
                new RowStyle(SizeType.AutoSize));
        }

        workoutLayout.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                50));

        workoutLayout.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                50));

        workoutBox.Controls.Add(workoutLayout);

        //
        // Axis mode
        //
        workoutLayout.Controls.Add(
            CreateLabel("Chart X Axis"),
            0,
            0);

        _cmbAxisMode.Items.AddRange(
        [
            "Time",
            "Distance"
        ]);

        _cmbAxisMode.SelectedIndex = 0;

        workoutLayout.Controls.Add(
            _cmbAxisMode,
            1,
            0);

        //
        // X range
        //
        workoutLayout.Controls.Add(
            CreateLabel("Range"),
            0,
            1);

        _numXAxis.Minimum = 1;

        _numXAxis.Maximum = 500;

        _numXAxis.Value = 30;

        workoutLayout.Controls.Add(
            _numXAxis,
            1,
            1);

        //
        // Min incline
        //
        workoutLayout.Controls.Add(
            CreateLabel("Min Incline"),
            0,
            2);

        _numMinIncline.Minimum = -20;

        _numMinIncline.Maximum = 20;

        _numMinIncline.DecimalPlaces = 1;

        _numMinIncline.Value = -3;

        workoutLayout.Controls.Add(
            _numMinIncline,
            1,
            2);

        //
        // Max incline
        //
        workoutLayout.Controls.Add(
            CreateLabel("Max Incline"),
            0,
            3);

        _numMaxIncline.Minimum = -20;

        _numMaxIncline.Maximum = 20;

        _numMaxIncline.DecimalPlaces = 1;

        _numMaxIncline.Value = 6;

        workoutLayout.Controls.Add(
            _numMaxIncline,
            1,
            3);

        //
        // Apply chart
        //
        _btnApplyChart.Text =
            "Apply Chart Settings";

        _btnApplyChart.Height = 42;

        _btnApplyChart.Width =
            (int)(workoutBox.Width * 0.90);

        _btnApplyChart.Anchor =
            AnchorStyles.None;

        _btnApplyChart.TextAlign =
            ContentAlignment.MiddleCenter;

        _btnApplyChart.Margin =
            new Padding(0, 10, 0, 10);

        workoutLayout.Controls.Add(
            _btnApplyChart,
            0,
            5);

        workoutLayout.SetColumnSpan(
            _btnApplyChart,
            2);

        //
        // Workout toggle button
        //
        _btnWorkoutToggle.Text =
            "START WORKOUT";

        _btnWorkoutToggle.Height = 50;

        _btnWorkoutToggle.Width =
            (int)(workoutBox.Width * 0.90);

        _btnWorkoutToggle.Anchor =
            AnchorStyles.None;

        _btnWorkoutToggle.TextAlign =
            ContentAlignment.MiddleCenter;

        _btnWorkoutToggle.BackColor =
            Color.LightGreen;

        _btnWorkoutToggle.Margin =
            new Padding(0, 10, 0, 10);

        workoutLayout.Controls.Add(
            _btnWorkoutToggle,
            0,
            6);

        workoutLayout.SetColumnSpan(
            _btnWorkoutToggle,
            2);

        left.Controls.Add(workoutBox);

        workoutBox.Resize += (_, _) =>
        {
            int buttonWidth =
                (int)(workoutBox.ClientSize.Width * 0.90);

            _btnApplyChart.Width =
                buttonWidth;

            _btnWorkoutToggle.Width =
                buttonWidth;
        };

        //
        // Status labels
        //
        left.Controls.Add(_lblStatus);

        left.Controls.Add(_lblConnection);

        left.Controls.Add(_lblSpeed);

        left.Controls.Add(_lblIncline);

        //
        // Workout runtime labels
        //
        _lblWorkoutTime.Text =
            "Workout Time: 00:00:00";

        _lblWorkoutTime.AutoSize = true;

        _lblWorkoutDistance.Text =
            "Distance: 0.00 km";

        _lblWorkoutDistance.AutoSize = true;

        left.Controls.Add(_lblWorkoutTime);

        left.Controls.Add(_lblWorkoutDistance);

        //
        // Chart
        //
        _chart.Dock = DockStyle.Fill;

        right.Controls.Add(_chart);
    }

    private static Label CreateLabel(
        string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Padding = new Padding(0, 10, 0, 5)
        };
    }

    #endregion
}