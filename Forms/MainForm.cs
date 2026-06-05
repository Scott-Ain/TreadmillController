using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WinForms;

using SkiaSharp;

using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;

using TreadmillController.Controllers;
using TreadmillController.Helpers;
using TreadmillController.Models;
using TreadmillController.Services;

namespace TreadmillController;

public sealed partial class MainForm : Form
{
    #region Services

    private readonly TreadmillFtmsService _ftms =
        new();

    private readonly WorkoutController _workoutController;

    #endregion

    #region Telemetry

    private readonly TelemetryState _telemetry =
        new();

    #endregion

    #region Workout Designer

    private readonly ObservableCollection<ObservablePoint>
        _workoutPoints = [];

    private readonly ObservableCollection<ObservablePoint>
    _progressLine = [];

    private AxisMode _axisMode =
        AxisMode.Time;

    private double _xAxisMaximum = 30;

    private double _minIncline = -3;

    private double _maxIncline = 6;

    #endregion

    #region Workout Runtime

    private readonly System.Windows.Forms.Timer
        _workoutTimer = new();

    private bool _workoutRunning;

    private double _elapsedSeconds;

    private double _distanceKm;

    private DateTime _lastWorkoutTick;

    #endregion

    #region Connection Monitor

    private readonly System.Windows.Forms.Timer
        _connectionTimer = new();

    private readonly TimeSpan _timeout =
        TimeSpan.FromSeconds(5);

    #endregion

    #region Drag Editing

    private bool _dragging;

    private ObservablePoint? _dragPoint;

    #endregion

    public MainForm()
    {
        InitializeComponent();

        _progressLine.Add(
            new ObservablePoint(0, _minIncline));

        _progressLine.Add(
            new ObservablePoint(0, _maxIncline));

        _workoutController =
            new WorkoutController(_workoutPoints);

        ConfigureChart();

        WireEvents();

        // Auto-discover treadmill when form appears (FTMS/TCP port scan)
        Shown += async (_, _) =>
        {
            try
            {
                _lblStatus.Text = "Scanning network...";
                _lblStatus.ForeColor = Color.DarkOrange;

                string? ip = await _ftms.DiscoverTreadmillAsync(TimeSpan.FromSeconds(5));

                if (ip != null)
                {
                    _txtIp.Text = ip;

                    _lblStatus.Text = $"Found treadmill: {ip}";
                    _lblStatus.ForeColor = Color.DarkGreen;
                }
                else
                {
                    _lblStatus.Text = "No treadmill found";
                    _lblStatus.ForeColor = Color.DarkRed;
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Discovery failed";
                _lblStatus.ForeColor = Color.DarkRed;
                Console.WriteLine(ex);
            }
        };

        ConfigureTimers();

        _workoutController.BuildDefaultWorkout(
            _xAxisMaximum);

        _chart.Update();

        RefreshDisplayedUnits();
    }

    private void WireEvents()
    {
        _rbKph.CheckedChanged += (_, _) =>
            RefreshDisplayedUnits();

        _rbMph.CheckedChanged += (_, _) =>
            RefreshDisplayedUnits();

        _ftms.RawTelemetryReceived +=
            Ftms_RawTelemetryReceived;

        _ftms.ErrorOccurred +=
            Ftms_ErrorOccurred;

        _btnUpdate.Click +=
            BtnUpdate_Click;

        _btnConnect.Click += async (_, _) =>
        {
            _btnConnect.Enabled = false;

            try
            {
                string ip = _txtIp.Text.Trim();

                if (_ftms.IsConnected)
                {
                    await _ftms.DisconnectFtmsAsync();

                    _btnConnect.Text = "Connect";
                    _lblStatus.ForeColor = Color.DarkRed;
                    _lblStatus.Text = "Disconnected";
                }
                else
                {
                    await _ftms.ConnectFtmsAsync(ip);

                    _btnConnect.Text = "Disconnect";
                    _lblStatus.ForeColor = Color.DarkGreen;
                    _lblStatus.Text = $"Connected to {ip}";
                }
            }
            catch (Exception ex)
            {
                _lblStatus.ForeColor = Color.DarkRed;
                _lblStatus.Text = $"Connect/Disconnect failed: {ex.Message}";
            }
            finally
            {
                _btnConnect.Enabled = true;
            }
        };

        _btnWorkoutToggle.Click +=
            BtnWorkoutToggle_Click;

        _btnApplyChart.Click += (_, _) =>
            ApplyChartSettings();

        _chart.MouseMove += Chart_MouseMove;
        _chart.MouseDown += Chart_MouseDown;
        _chart.MouseUp += Chart_MouseUp;
    }

    private void ConfigureTimers()
    {
        _connectionTimer.Interval = 1000;

        _connectionTimer.Tick += (_, _) =>
            UpdateConnectionStatus();

        _connectionTimer.Start();

        _workoutTimer.Interval = 1000;

        _workoutTimer.Tick +=
            WorkoutTimer_Tick;
    }
}